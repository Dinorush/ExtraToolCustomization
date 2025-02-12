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
            }
        };
    }
}
