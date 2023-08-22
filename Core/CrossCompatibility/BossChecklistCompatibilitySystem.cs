using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using static NoxusBoss.Core.CrossCompatibility.ModReferences;

namespace NoxusBoss.Core.CrossCompatibility
{
    public class BossChecklistCompatibilitySystem : ModSystem
    {
        public override void PostSetupContent()
        {
            // Don't load anything if boss checklist is not enabled.
            if (BossChecklist is null)
                return;

            // Collect all NPCs that should adhere to boss checklist.
            var modNPCsWithBossChecklistSupport = Mod.GetContent().Where(c =>
            {
                return c is ModNPC and IBossChecklistSupport;
            }).Select(c => c as ModNPC);

            // Load boss checklist information via mod calls.
            foreach (var modNPC in modNPCsWithBossChecklistSupport)
            {
                IBossChecklistSupport checklistInfo = modNPC as IBossChecklistSupport;
                string registerCall = checklistInfo.IsMiniboss ? "LogMiniBoss" : "LogBoss";

                Dictionary<string, object> extraInfo = new()
                {
                    ["collectibles"] = checklistInfo.Collectibles
                };
                if (checklistInfo.SpawnItem is not null)
                    extraInfo["spawnItems"] = checklistInfo.SpawnItem.Value;
                if (checklistInfo.UsesCustomPortraitDrawing)
                    extraInfo["customPortrait"] = new Action<SpriteBatch, Rectangle, Color>(checklistInfo.DrawCustomPortrait);

                // Use the mod call.
                string result = (string)BossChecklist.Call(new object[]
                {
                    registerCall,
                    Mod,
                    checklistInfo.ChecklistEntryName,
                    checklistInfo.ProgressionValue,
                    () => checklistInfo.IsDefeated,
                    modNPC.Type,
                    extraInfo
                });
            }
        }
    }
}
