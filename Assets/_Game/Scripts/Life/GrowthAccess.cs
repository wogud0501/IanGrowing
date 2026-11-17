namespace IanGame
{
    // One execution-time policy. Ages derive from the existing growth model.
    // Independent travel from Childhood was explicitly confirmed by the user on 2026-11-13.
    public static class GrowthAccess
    {
        public static bool Independent(GameManager g) => g != null && g.growth.IndependentOutdoorUnlocked;

        public static bool Adult(GameManager g) => g != null && g.growth.FinanceUnlocked;

        public static bool Zone(GameManager g, string zone, out string reason)
        {
            reason = null;
            if (g == null)
            {
                reason = "성장 정보를 불러오는 중입니다.";
                return false;
            }

            if (FamilyLife.IsFamilyRoom(zone))
            {
                return true;
            }

            if (zone == "home" || zone == "bathroom")
            {
                if (!new ResidenceAccess(g).IsActiveResidence)
                {
                    reason = "집 관리에서 19세 이후 독립을 선택하세요. 부모님 집은 계속 이용할 수 있습니다.";
                    return false;
                }

                return true;
            }

            if (!Independent(g))
            {
                reason = "독립 외출은 아동기(7세)부터 가능합니다. 집에서 돌봄과 성장 미션을 진행하세요.";
                return false;
            }

            if ((zone == "workplace" || zone == "office-bathroom") && !g.growth.EmploymentUnlocked)
            {
                reason = "회사 이용은 청년기(19세)부터 해금됩니다.";
                return false;
            }

            if ((zone == "finance" || zone == "realestate") && !Adult(g))
            {
                reason = "금융·주거 계약은 청년기(19세)부터 해금됩니다.";
                return false;
            }

            if (
                zone == "school"
                && (
                    g.phase23.Education.Available == null
                    || g.growth.Stage < g.phase23.Education.Available.requiredStage
                )
            )
            {
                reason = "현재 성장 단계에 맞는 교육 과정이 없습니다.";
                return false;
            }

            return true;
        }

        public static bool Scene(GameManager g, SceneId scene, out string reason)
        {
            var room = GameContext.Instance?.transitions.roomCatalog?.Find(scene);
            if (room != null)
            {
                return Zone(g, room.roomId, out reason);
            }

            string zone = scene == SceneId.Home ? FamilyLife.DefaultHome(g) : LifeWorldService.ZoneFor(scene);
            if (zone != null)
            {
                return Zone(g, zone, out reason);
            }

            if (scene == SceneId.StockMarket || scene == SceneId.Housing)
            {
                reason = Adult(g) ? null : "금융·주거 계약은 청년기(19세)부터 해금됩니다.";
                return reason == null;
            }

            if (scene == SceneId.Battle || scene == SceneId.Shop)
            {
                reason = Independent(g) ? null : "독립 외출은 아동기(7세)부터 가능합니다.";
                return reason == null;
            }

            reason = null;
            return true;
        }

        public static bool Interaction(GameManager g, WorldInteractable o, int action, out string reason)
        {
            reason = null;
            if (o == null)
            {
                return false;
            }

            if (o.kind == WorldInteractionKind.Door)
            {
                if (g != null && !g.growth.WalkingUnlocked)
                {
                    reason = "아직 혼자 문을 통과할 수 없습니다. 보호자의 도움이 필요합니다.";
                    return false;
                }

                return Zone(g, o.targetZone, out reason);
            }

            if (!Zone(g, o.zone, out reason))
            {
                return false;
            }

            bool adult =
                o.kind == WorldInteractionKind.Work
                || o.kind == WorldInteractionKind.StockMarket
                || o.kind == WorldInteractionKind.RealEstate
                || action == -3;
            if (adult && !Adult(g))
            {
                reason = "현재 성장 단계에서는 이용할 수 없습니다. 청년기(19세)에 해금됩니다.";
                return false;
            }

            if (FamilyLife.IsFamilyRoom(o.zone) && o.kind == WorldInteractionKind.Shower && g.growth.Age <= 2)
            {
                reason = "아직 보호자의 목욕 도움이 필요합니다. 아기 침대에서 돌봄을 선택하세요.";
                return false;
            }

            if (action == -4 && !(o.id == "family-bed" && FamilyLife.IsFamilyRoom(o.zone) && g.growth.Age <= 2))
            {
                reason = "영유아 목욕 돌봄은 가족 침대에서 이용하세요.";
                return false;
            }

            if (FamilyLife.IsFamilyRoom(o.zone))
            {
                if (ParentCare(g, o, action))
                {
                    return true;
                }

                if (g.growth.Age == 0)
                {
                    reason = "부모님이나 아기 침대에서 돌봄을 받으세요.";
                    return false;
                }
            }

            if (
                !Independent(g)
                && !FamilyLife.IsFamilyRoom(o.zone)
                && o.kind != WorldInteractionKind.Sleep
                && o.kind != WorldInteractionKind.Rest
            )
            {
                reason = "현재는 돌봄을 받는 시기입니다. 침대에서 돌봄 행동을 선택하세요.";
                return false;
            }

            return true;
        }

        static bool ParentCare(GameManager g, WorldInteractable o, int action) =>
            g.growth.Age == 0 && (o.id == "family-bed" || FamilyLife.Parent(o.npcId)) && (action < 0 || Care(action));

        public static bool Care(int action) => action >= 1 && action <= 6 || action == 17;
    }
}
