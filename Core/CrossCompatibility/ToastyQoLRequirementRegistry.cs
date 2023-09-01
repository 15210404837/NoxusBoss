using CalamityMod;

namespace NoxusBoss.Core.CrossCompatibility
{
    public static class ToastyQoLRequirementRegistry
    {
        public static readonly ToastyQoLRequirement PostDraedonAndCal = new("Endgame", () => DownedBossSystem.downedExoMechs && DownedBossSystem.downedCalamitas);

        public static readonly ToastyQoLRequirement PostNoxus = new("Entropic God", () => WorldSaveSystem.HasDefeatedNoxus);

        public static readonly ToastyQoLRequirement PostXeroc = new("Nameless Deity", () => WorldSaveSystem.HasDefeatedXeroc);
    }
}
