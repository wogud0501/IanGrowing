using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IanGame
{
    public enum CityTravelMode
    {
        Walk,
        Subway,
        Taxi,
        Bus,
    }

    public sealed partial class GameplayUXPresenter
    {
        const string CutKey = "ian.transit.cutLength";
        public const int SubwayFare = 1400;
        public const int BusFare = 1200;
        public const int TaxiFare = 15000;

        GameObject timetable;
        WorldInteractable transitEntrance;
        TMP_Text stationName;
        TMP_Text lineMap;
        Button walking;
        Button railway;
        Button cab;
        Button bus;
        TMP_Text cutPreference;
        bool departing;
        bool skipRide;

        internal static int CutLength => Mathf.Clamp(PlayerPrefs.GetInt(CutKey), 0, 2);
        internal static string CutLabel =>
            CutLength == 0 ? "항상 보기"
            : CutLength == 1 ? "짧게 보기"
            : "건너뛰기";

        internal static void CycleCutLength()
        {
            PlayerPrefs.SetInt(CutKey, (CutLength + 1) % 3);
            PlayerPrefs.Save();
        }

        public bool OfferTransport(WorldInteractable entrance)
        {
            if (!Ready || departing || !entrance)
            {
                return false;
            }
            transitEntrance = entrance;
            if (!timetable)
            {
                BuildTimetable();
            }
            bool crossing = entrance.id is "city-path" or "alley-path";
            bool residential = entrance.zone is "alley" or "station-alley";
            stationName.text = residential ? "목적지  도심" : "목적지  주거지역";
            lineMap.text = residential ? "● 01 주거지역   ━━━   ○ 02 도심" : "○ 01 주거지역   ━━━   ● 02 도심";
            walking.gameObject.SetActive(crossing);
            Configure(
                railway,
                crossing,
                entrance.id == "platform",
                "지하철역 입구까지 걷기",
                "지하철 승차  ·  1,400 G / 멘탈 −2",
                SubwayFare
            );
            Configure(
                bus,
                crossing,
                entrance.id == "bus-stop",
                "버스 정류장까지 걷기",
                "02번 버스 승차  ·  1,200 G",
                BusFare
            );
            Configure(
                cab,
                crossing,
                entrance.id == "taxi-rank",
                "택시 승차대까지 걷기",
                "택시 호출  ·  15,000 G",
                TaxiFare
            );
            cutPreference.text = "이동 연출  ·  " + CutLabel;
            Open(timetable);
            return true;
        }

        void Configure(Button button, bool crossing, bool atStop, string directions, string ticket, int fare)
        {
            button.gameObject.SetActive(crossing || atStop);
            button.GetComponentInChildren<TMP_Text>(true).text = crossing ? directions : ticket;
            button.interactable = crossing || game.economy.Cash >= fare;
        }

        void BuildTimetable()
        {
            timetable = Window("교통 · 노선도");
            var t = timetable.transform;
            stationName = ui.Text(t, "Destination", 24, new Vector2(.08f, .80f), new Vector2(.92f, .89f));
            lineMap = ui.Text(t, "Stops", 19, new Vector2(.08f, .70f), new Vector2(.92f, .80f));
            walking = Choice("도보  ·  무료", .57f, CityTravelMode.Walk);
            railway = Choice("지하철", .445f, CityTravelMode.Subway);
            bus = Choice("버스", .32f, CityTravelMode.Bus);
            cab = Choice("택시", .195f, CityTravelMode.Taxi);
            cutPreference = ui.Button(
                    t,
                    "이동 연출",
                    new Vector2(.08f, .05f),
                    new Vector2(.92f, .15f),
                    () =>
                    {
                        CycleCutLength();
                        cutPreference.text = "이동 연출  ·  " + CutLabel;
                    }
                )
                .GetComponentInChildren<TMP_Text>();
        }

        Button Choice(string title, float y, CityTravelMode mode)
        {
            return ui.Button(
                timetable.transform,
                title,
                new Vector2(.08f, y),
                new Vector2(.92f, y + .102f),
                () => Ride(mode)
            );
        }

        void Ride(CityTravelMode mode)
        {
            if (departing || shell.Top != timetable || !transitEntrance)
            {
                return;
            }
            var source = transitEntrance;
            shell.Close(timetable);
            if (!hud.world.CanControl || !hud.world.IsInReach(source))
            {
                return;
            }
            if (mode == CityTravelMode.Walk)
            {
                hud.world.WalkThrough(source);
                return;
            }
            StartCoroutine(Trip(source, mode));
        }

        IEnumerator Trip(WorldInteractable source, CityTravelMode mode)
        {
            var world = hud.world;
            departing = true;
            GameObject sequence = null;
            StationTrainDoors train = null;
            try
            {
                if (source.id is "city-path" or "alley-path")
                {
                    string id =
                        mode == CityTravelMode.Subway ? "subway-entrance"
                        : mode == CityTravelMode.Bus ? "bus-stop"
                        : "taxi-rank";
                    var stop = world.objects.FirstOrDefault(o => o && o.id == id && o.zone == source.zone);
                    yield return world.WalkToTransit(stop);
                    departing = false;
                    if (!stop || !world.Use(stop))
                    {
                        hud.Notify("승차 지점 앞으로 이동해 주세요.");
                    }
                    yield break;
                }

                var portal = world.Room.catalog.portals.FirstOrDefault(p =>
                    p.sourceRoom == source.zone && p.doorId == source.id
                );
                if (portal == null)
                {
                    hud.Notify("등록된 노선이 없습니다.");
                    yield break;
                }
                yield return world.WalkToTransit(source);
                if (!world.CanControl || Vector2.Distance(world.Player.Position, source.Point) > .38f)
                {
                    hud.Notify("승차 지점이 막혀 있습니다.");
                    yield break;
                }
                world.Player.Face(Vector2.up);
                if (mode == CityTravelMode.Subway)
                {
                    train = source.GetComponent<StationTrainDoors>();
                    if (!train)
                    {
                        yield break;
                    }
                    train.HoldForBoarding(true);
                    float until = Time.unscaledTime + 1.5f;
                    while (train.Aperture < .96f && Time.unscaledTime < until)
                    {
                        yield return null;
                    }
                    if (train.Aperture < .96f)
                    {
                        yield break;
                    }
                }
                if (CutLength < 2)
                {
                    sequence = ui.Panel("이동", shell.overlayRoot, Vector2.zero, Vector2.one).gameObject;
                    var stage = TransitRideView.Create(sequence.transform, mode, world.Player, ui);
                    skipRide = false;
                    ui.Button(
                        sequence.transform,
                        "건너뛰기",
                        new Vector2(.60f, .09f),
                        new Vector2(.92f, .17f),
                        () => skipRide = true
                    );
                    shell.Push(sequence);
                    float duration =
                        CutLength == 1 ? 1.75f
                        : mode == CityTravelMode.Subway ? 3.15f
                        : 3f;
                    for (float t = 0; t < duration && !skipRide && shell.Top == sequence; t += Time.unscaledDeltaTime)
                    {
                        stage.Sample(t / duration);
                        yield return null;
                    }
                    if (shell.Top != sequence)
                    {
                        yield break;
                    }
                    stage.Sample(1);
                    shell.Close(sequence);
                    while (sequence && sequence.activeSelf)
                    {
                        yield return null;
                    }
                }
                if (
                    !GameContext.Instance.transitions.NavigateRoom(
                        portal.destinationRoom,
                        portal.arrival,
                        portal.facing,
                        portal.destinationEntryId,
                        mode
                    )
                )
                {
                    hud.Notify("이동할 수 없습니다. 운임은 결제되지 않았습니다.");
                }
            }
            finally
            {
                if (train)
                {
                    train.HoldForBoarding(false);
                }
                if (sequence)
                {
                    Destroy(sequence);
                }
                departing = false;
            }
        }
    }
}
