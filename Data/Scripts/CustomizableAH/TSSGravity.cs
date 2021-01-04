using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using Gwindalmir.RealTimeTSS;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace CustomizableAH
{
    [MyTextSurfaceScript("TSS_Gravity", "DisplayName_TSS_Gravity")]
    public class TSSGravity : TSSCommon
    {
        public const float G = 9.81f;

        public TSSGravity(IMyTextSurface surface, VRage.Game.ModAPI.Ingame.IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            m_sb.Clear();
            m_sb.Append(MyTexts.Get(MySpaceTexts.AGravity));
            m_sb.Append(": 00.00g");
            var textSize = m_surface.MeasureStringInPixels(m_sb, MyTextSurfaceHelper.DEFAULT_FONT_ID, 1f);
            var fontSize = (TEXT_RATIO * m_innerSize.Y) / textSize.Y;
            m_fontScale = Math.Min(m_innerSize.X * 0.72f / textSize.X, fontSize);

            m_updateRateDivisor = 2;    // 15 FPS
        }

        public override void Draw(MySpriteDrawFrame frame)
        {
            AddBackground(frame, new Color(m_backgroundColor, .66f));

            if (m_block == null)
                return;

            var pos = m_block.GetPosition();
            float naturalMultiplier;

            var naturalGravityVector = MyAPIGateway.Physics.CalculateNaturalGravityAt(pos, out naturalMultiplier);
            var artificialGravityVector = MyAPIGateway.Physics.CalculateArtificialGravityAt(pos, naturalMultiplier);
            var g = artificialGravityVector.Length() / G;

            m_sb.Clear();
            m_sb.Append(MyTexts.Get(MySpaceTexts.AGravity));
            m_sb.AppendFormat(": {0:F2}g", g);
            var size = m_surface.MeasureStringInPixels(m_sb, m_fontId, m_fontScale);

            var artGravityText = new MySprite()
            {
                Position = new Vector2(m_halfSize.X, m_firstLine - size.Y * 0.5f),
                Size = new Vector2(m_innerSize.X, m_innerSize.Y),
                Type = SpriteType.TEXT,
                FontId = m_fontId,
                Alignment = TextAlignment.CENTER,
                Color = m_foregroundColor,
                RotationOrScale = m_fontScale,
                Data = m_sb.ToString(),
            };
            frame.Add(artGravityText);

            g = naturalGravityVector.Length() / G;

            m_sb.Clear();
            m_sb.Append(MyTexts.Get(MySpaceTexts.PGravity));
            m_sb.AppendFormat(": {0:F2}g", g);
            size = m_surface.MeasureStringInPixels(m_sb, m_fontId, m_fontScale);
            var natGravityText = new MySprite()
            {
                Position = new Vector2(m_halfSize.X, m_secondLine - size.Y * 0.5f),
                Size = new Vector2(m_innerSize.X, m_innerSize.Y),
                Type = SpriteType.TEXT,
                FontId = m_fontId,
                Alignment = TextAlignment.CENTER,
                Color = m_foregroundColor,
                RotationOrScale = m_fontScale,
                Data = m_sb.ToString(),
            };
            frame.Add(natGravityText);

            var sideScale = m_innerSize.Y / 256 * 0.9f;
            var offset = (m_size.X - m_innerSize.X) / 2;

            AddBrackets(frame, new Vector2(64, 256), sideScale, offset);
        }
    }
}
