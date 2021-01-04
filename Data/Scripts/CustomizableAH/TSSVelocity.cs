using Sandbox.Definitions;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace Gwindalmir.RealTimeTSS
{
    [MyTextSurfaceScript("TSS_Velocity", "DisplayName_TSS_Velocity")]
    public class TSSVelocity : TSSCommon
    {
        private IMyCubeGrid m_grid;

        public TSSVelocity(IMyTextSurface surface, IMyCubeBlock block, Vector2 size)
            : base(surface, block, size)
        {
            if (m_block != null)
                m_grid = m_block.CubeGrid as IMyCubeGrid;

            m_updateRateDivisor = 1;    // 30 FPS
        }

        public override List<MySprite> RunSpecial()
        {
            // Slow down updates if nothing changed (so display properly refreshes with accurate info on stop)
            if (m_lastBlockMatrix == m_block.WorldMatrix)
            {
                if (m_updateRateDivisor != 30)
                    m_updateRateDivisor = 30;   // 1 FPS
            }
            else
            {
                if (m_updateRateDivisor != 1)
                    m_updateRateDivisor = 1;    // Back to 30 FPS
            }

            return base.RunSpecial();
        }

        public override void Draw(MySpriteDrawFrame frame)
        {
            AddBackground(frame, new Color(m_backgroundColor, .66f));

            if (m_grid == null || m_grid.Physics == null)
                return;

            var barBgColor = new Color(m_foregroundColor, 0.1f);
            var velocity = (m_grid as IMyCubeGrid).Physics.LinearVelocity.Length();
            var maxV = Math.Max(Math.Max(MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed,
                                         MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed),
                                1f);
            var percentage = velocity / maxV;

            var text = string.Format("{0:F2} m/s", velocity);

            var size = m_surface.MeasureStringInPixels(new StringBuilder(text), m_fontId, m_fontScale);

            var velocityText = new MySprite()
            {
                Position = new Vector2(m_halfSize.X, m_firstLine - size.Y * 0.5f),
                Size = new Vector2(m_innerSize.X, m_innerSize.Y),
                Type = SpriteType.TEXT,
                FontId = m_fontId,
                Alignment = TextAlignment.CENTER,
                Color = m_foregroundColor,
                RotationOrScale = m_fontScale,
                Data = text,
            };
            frame.Add(velocityText);

            m_sb.Clear();
            m_sb.Append("[");
            var textSize = m_surface.MeasureStringInPixels(m_sb, m_fontId, 1f);
            var scale = m_decorationSize.Y / textSize.Y;
            textSize = m_surface.MeasureStringInPixels(m_sb, m_fontId, scale);

            var width = m_innerSize.X * 0.6f;

            AddProgressBar(frame, new Vector2(m_halfSize.X, m_secondLine), new Vector2(width, textSize.Y * 0.4f), percentage, barBgColor, m_foregroundColor);

            var sideScale = m_innerSize.Y / 256 * 0.9f;
            var offset = (m_size.X - m_innerSize.X) / 2;

            AddBrackets(frame, new Vector2(64, 256), sideScale, offset);
        }

        public override void Dispose()
        {
            base.Dispose();

            m_grid = null;
        }
    }
}
