using UnityEngine;

namespace ExtraToolCustomization.ToolData.Templates
{
    internal static class MineTemplate
    {
        public static MineData[] Template = new MineData[]
        {
            new()
            {
                Name = "Mine Deployer",
                Delay = 0.25f,
                Radius = 2.5f,
                DistanceMin = 3f,
                DistanceMax = 15f,
                DamageMin = 15f,
                DamageMax = 50f,
                Force = 1000f,
                BeamData = new()
                {
                    Color = Color.red,
                    Length = 20f,
                    Width = 1f
                },
                LightData = new()
                {
                    Color = Color.red,
                    Range = 1f,
                    Intensity = 0.03f
                }
            },
            new()
            {
                Name = "Consumable Mine",
                Delay = 0.25f,
                Radius = 2f,
                DistanceMin = 2.5f,
                DistanceMax = 12f,
                DamageMin = 10f,
                DamageMax = 35f,
                Force = 700f,
                BeamData = new()
                {
                    Color = Color.red,
                    Length = 20f,
                    Width = 1f
                },
                LightData = new()
                {
                    Color = new Color(1f, 0.12554f, 0.12554f),
                    Range = 0.8f,
                    Intensity = 0.3f
                }
            },
            new()
            {
                Name = "Consumable Foam Mine",
                Delay = 0f,
                Radius = 2f,
                DistanceMin = 2.5f,
                DistanceMax = 12f,
                DamageMin = 10f,
                DamageMax = 35f,
                Force = 700f,
                FoamData = new()
                {
                    BubbleDelay = 0f,
                    BubbleCount = 17,
                    BubbleBatchCooldown = 0.25f,
                    BubbleAngle = 14f,
                    BubbleSpeedMin = 7f,
                    BubbleSpeedMax = 13f
                },
                BeamData = new()
                {
                    Color = new Color(0f, 0.38432f, 1f),
                    Length = 20f,
                    Width = 1f
                },
                LightData = new()
                {
                    Color = new Color(0.1255f, 0.549f, 1f),
                    Range = 0.8f,
                    Intensity = 0.3f
                }
            }
        };
    }
}
