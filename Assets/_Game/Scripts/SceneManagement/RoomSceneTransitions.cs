using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IanGame
{
    public partial class SceneTransitionService
    {
        public RoomSceneCatalog roomCatalog;
        public bool LegacyTesting { get; private set; }
        public bool IsRoomArchitecture => roomCatalog != null && !LegacyTesting;

        bool TryRoomNavigation(SceneId id, out bool accepted)
        {
            accepted = false;
            var game = GameContext.Instance?.game;
            if (game == null)
            {
                return false;
            }

            var direct = roomCatalog.Find(id);
            string zone = LifeWorldService.ZoneFor(id);
            if (direct == null && zone == null && id != SceneId.LifeWorld)
            {
                return false;
            }

            game.world.UseRoomRules(roomCatalog);
            string desired = direct?.roomId;
            bool resume = id == SceneId.LifeWorld || id == SceneId.Home && CurrentScene == SceneId.MainMenu;
            if (id == SceneId.RoomResidence && roomCatalog.Find(game.world.State.zone)?.sceneId == id)
            {
                desired = game.world.State.zone;
                resume = true;
            }

            if (desired == null)
            {
                desired =
                    resume ? game.world.State.zone
                    : id == SceneId.Home ? FamilyLife.DefaultHome(game)
                    : zone;
            }

            var position = resume ? game.world.State.position : game.world.rules.Zone(desired)?.spawn ?? Vector2.zero;
            var room = roomCatalog.Resolve(desired, position, game.growth.Age);
            if (room == null)
            {
                Fail("등록된 공간이 없습니다: " + desired);
                return true;
            }

            if (desired != room.roomId)
            {
                position -= room.legacyOffset;
            }

            accepted = NavigateRoom(room.roomId, position, game.world.State.facing);
            return true;
        }

        public bool NavigateRoom(string roomId, Vector2 arrival, Vector2 facing, string entryId = "", CityTravelMode mode = CityTravelMode.Walk)
        {
            if (IsTransitioning || roomCatalog == null)
            {
                return false;
            }

            var game = GameContext.Instance?.game;
            var room = roomCatalog.Find(roomId);
            if (game == null || room == null || !GrowthAccess.Zone(game, roomId, out var denied))
            {
                Fail(room == null ? "등록되지 않은 공간입니다." : "이 성장 단계에서는 출입할 수 없습니다.");
                return false;
            }

            int fare = 0;
            string origin = game.world.Host ? game.world.Host.Room?.roomId : null;
            if (mode == CityTravelMode.Subway && ((origin == "station-alley" && roomId == "station-city")
                || (origin == "station-city" && roomId == "station-alley")))
            {
                fare = GameplayUXPresenter.SubwayFare;
            }
            else if ((mode == CityTravelMode.Taxi || mode == CityTravelMode.Bus) && ((origin == "alley" && roomId == "city")
                || (origin == "city" && roomId == "alley")))
            {
                fare = mode == CityTravelMode.Bus ? GameplayUXPresenter.BusFare : GameplayUXPresenter.TaxiFare;
            }
            else if (mode != CityTravelMode.Walk)
            {
                return false;
            }

            if (game.economy.Cash < fare)
            {
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(room.sceneName))
            {
                Fail("씬이 빌드 목록에 없습니다: " + room.sceneName);
                return false;
            }

            if (
                game.State == GameState.Resolving
                || game.BirthIntroductionPending
                    && CurrentScene != SceneId.MainMenu
                    && !(roomId == "family-ian-bedroom" && game.growth.Age == 0)
            )
            {
                return false;
            }

            if (CurrentScene == room.sceneId && SceneManager.GetActiveScene().name == room.sceneName
                && game.world.Host?.Room.roomId == room.roomId)
            {
                if (game.world.Host != null)
                {
                    game.world.Host.ApplySavedState();
                }

                return true;
            }

            LegacyTesting = false;
            LastError = null;
            IsTransitioning = true;
            StartCoroutine(LoadRoom(room, arrival, facing, entryId, fare, mode));
            return true;
        }

        IEnumerator LoadRoom(RoomSceneDefinition room, Vector2 arrival, Vector2 facing, string entryId, int fare, CityTravelMode mode)
        {
            var game = GameContext.Instance.game;
            var source = game.world.Host;
            var prior = SceneManager.GetActiveScene();
            var priorId = CurrentScene;
            var before = game.CaptureState();
            bool running = game.clock.IsRunning;
            game.clock.SetRunning(false);
            game.world.Host?.GetComponent<WorldNPCScheduler>()?.Capture();
            if (loadingText != null)
            {
                loadingText.text = room.title + " 이동 중";
            }

            yield return null;
            if (source != null)
            {
                source.hud.CloseContext();
                while (source.hud.shell.Depth > 0)
                {
                    int depth = source.hud.shell.Depth;
                    source.hud.shell.Pop();
                    if (source.hud.shell.Depth == depth)
                    {
                        break;
                    }
                }

                source.CancelAction();
                source.Player.CancelTraversal();
                source.transitions?.Begin(source.CurrentZone.id, room.roomId);
                for (float t = 0; t < .16f; t += Time.unscaledDeltaTime)
                {
                    source.transitions?.Cover(t / .16f);
                    yield return null;
                }

                source.transitions?.Cover(1);
            }

            if (loadingOverlay != null)
            {
                loadingOverlay.SetActive(true);
            }

            AsyncOperation operation = null;
            try
            {
                operation = SceneManager.LoadSceneAsync(room.sceneName, LoadSceneMode.Additive);
                if (operation != null)
                {
                    operation.allowSceneActivation = false;
                }
            }
            catch (Exception e)
            {
                LastError = e.Message;
            }

            if (operation == null)
            {
                source?.transitions?.End();
                Fail(LastError ?? "씬 로딩 시작 실패");
                Finish(game, running);
                yield break;
            }

            while (operation.progress < .9f && !operation.isDone)
            {
                yield return null;
            }

            // Staging is additive, but only one room's input/camera/player/UI can be active.
            var roots = prior.GetRootGameObjects();
            foreach (var root in roots)
            {
                root.SetActive(false);
            }

            game.world.UseRoomRules(roomCatalog);
            game.world.State.zone = room.roomId;
            game.world.State.position = arrival;
            game.world.State.facing = facing;
            game.world.State.pendingAction = "";
            operation.allowSceneActivation = true;
            while (!operation.isDone)
            {
                yield return null;
            }

            var destination = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt)
                .FirstOrDefault(scene => scene != prior && scene.name == room.sceneName);
            if (
                !destination.IsValid()
                || !destination.isLoaded
                || game.world.Host == null
                || game.world.Host.gameObject.scene != destination
                || (fare > 0 && !game.economy.TrySpend(fare, "교통비"))
            )
            {
                foreach (var root in roots)
                {
                    if (root != null)
                    {
                        root.SetActive(true);
                    }
                }

                game.RestoreState(before);
                CurrentScene = priorId;
                Fail("공간 연결에 실패해 이전 상태를 복구했습니다.");
                if (destination.IsValid() && destination.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(destination);
                }

                Finish(game, running);
                yield break;
            }

            if (mode == CityTravelMode.Subway)
            {
                game.stats.ApplyCore(new StatDelta { mental = -2 });
            }

            SceneManager.SetActiveScene(destination);
            CurrentScene = room.sceneId;
            var host = game.world.Host;
            if (!string.IsNullOrEmpty(entryId))
            {
                var entry = host.GetComponentsInChildren<RoomEntryPoint>().FirstOrDefault(p => p.entryId == entryId);
                if (entry != null)
                {
                    game.world.State.position = (Vector2)entry.transform.position - host.CurrentZone.origin;
                    game.world.State.facing = entry.facing;
                    arrival = game.world.State.position;
                    facing = entry.facing;
                }
            }

            if (prior.IsValid() && prior.isLoaded)
            {
                var unload = SceneManager.UnloadSceneAsync(prior);
                if (unload != null)
                {
                    yield return unload;
                }
            }

            yield return null;
            var door = host
                .objects.Where(o => o.kind == WorldInteractionKind.Door)
                .Select(o => o.GetComponent<WorldDoor>())
                .Where(d => d != null)
                .OrderBy(d => Vector2.Distance(d.Plane, host.CurrentZone.origin + arrival))
                .FirstOrDefault();
            if (door != null && Vector2.Distance(door.Plane, host.CurrentZone.origin + arrival) < 2)
            {
                door.Acquire("player-arrival", game.world.Hour, out _);
                float until = Time.unscaledTime + 1;
                while (door.State != DoorState.Open && Time.unscaledTime < until)
                {
                    yield return null;
                }
            }

            host.Player.Warp(host.Safe(host.CurrentZone, arrival));
            host.Player.Face(facing);
            host.Player.GetComponent<Rigidbody2D>().simulated = true;
            host.CapturePosition();
            host.follow.Snap();
            door?.Release("player-arrival");
            host.transitions?.Begin(room.roomId, room.roomId);
            host.transitions?.Cover(1);
            if (loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }

            for (float t = 0; t < .2f; t += Time.unscaledDeltaTime)
            {
                host.transitions?.Cover(1 - t / .2f);
                yield return null;
            }

            host.transitions?.End();
            Finish(game, true);
            SceneChanged?.Invoke(room.sceneId);
            game.SaveAutomaticCheckpoint(out _);
        }
    }
}
