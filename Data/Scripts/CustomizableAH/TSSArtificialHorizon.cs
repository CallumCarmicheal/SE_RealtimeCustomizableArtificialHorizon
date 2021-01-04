/* Portions Copyright © 2020 Gwindalmir */
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace Gwindalmir.RealTimeTSS
{
    [MyTextSurfaceScript("TSS_ArtificialHorizon", "DisplayName_TSS_ArtificialHorizon")]
    public class TSSArtificialHorizon : TSSCommon
    {
        private const int HUD_SCALING = 1200;
        private const double PLANET_GRAVITY_THRESHOLD_SQ = 0.0025;
        private const float LADDER_TEXT_SIZE_MULTIPLIER = 0.7f;
        private const int ALTITUDE_WARNING_TIME_THRESHOLD = 24;
        private const int RADAR_ALTITUDE_THRESHOLD = 500;

        private static readonly float ANGLE_STEP = (MathHelper.PiOver2 / 18);

        private IMyCubeGrid m_grid;
        private MatrixD m_ownerTransform;

        private float m_maxScale;
        private float m_screenDiag;

        private int m_tickCounter = 0;

        private int m_lastRadarAlt = 0;
        private double m_lastSeaLevelAlt = 0;
        private bool m_showAltWarning = false;
        private int m_altWarningShownAt;
        private Vector2 m_textBoxSize;
        private Vector2 m_textOffsetInsideBox;
        private Vector2 m_ladderStepSize;
        private Vector2 m_ladderStepTextOffset;

        private MyPlanet m_nearestPlanet;

        public TSSArtificialHorizon(IMyTextSurface surface, VRage.Game.ModAPI.Ingame.IMyCubeBlock block, Vector2 size)
            : base(surface, block, size)
        {
            if (m_block != null)
                m_grid = m_block.CubeGrid as IMyCubeGrid;

            m_maxScale = Math.Min(m_scale.X, m_scale.Y);

            m_innerSize = new Vector2(1.2f, 1f);
            FitRect(m_surface.SurfaceSize, ref m_innerSize);

            m_screenDiag = (float)Math.Sqrt(m_innerSize.X * m_innerSize.X + m_innerSize.Y * m_innerSize.Y);

            m_fontScale = 1.0f * m_maxScale;
            m_fontId = "White";

            m_ownerTransform = m_grid.PositionComp.WorldMatrixRef;
            m_ownerTransform.Translation = m_block.GetPosition();

            m_nearestPlanet = MyGamePruningStructure.GetClosestPlanet(m_ownerTransform.Translation);

            m_textBoxSize = new Vector2(89, 32) * m_maxScale;
            m_textOffsetInsideBox = new Vector2(5, 0) * m_maxScale;

            m_ladderStepSize = new Vector2(150, 31f) * m_maxScale;
            m_ladderStepTextOffset = new Vector2(0, m_ladderStepSize.Y * .5f);

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
            if (m_grid == null || m_grid.Physics == null)
                return;

            Matrix blockLocalMat;
            m_block.Orientation.GetMatrix(out blockLocalMat);

            m_ownerTransform = blockLocalMat * m_grid.PositionComp.WorldMatrixRef;
            m_ownerTransform.Translation = m_block.GetPosition();
            m_ownerTransform.Orthogonalize();

            float interference;
            var gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(m_ownerTransform.Translation, out interference);
            if (gravity.LengthSquared() >= PLANET_GRAVITY_THRESHOLD_SQ)
                DrawPlanetDisplay(frame, gravity, m_ownerTransform);
            else
                DrawSpaceDisplay(frame, m_ownerTransform);

            m_tickCounter++;
        }

        #region Planet Display
        private void DrawPlanetDisplay(MySpriteDrawFrame frame, Vector3 gravity, MatrixD worldTrans)
        {
            gravity.Normalize();

            var horizonForward = Vector3D.Reject(worldTrans.Forward, gravity);
            horizonForward.Normalize();
            var screenForward = Vector3D.Reject(horizonForward, worldTrans.Forward);
            screenForward = Vector3D.TransformNormal(screenForward, MatrixD.Invert(worldTrans));
            var screenForward2D = new Vector2((float)screenForward.X, -(float)screenForward.Y) * HUD_SCALING * m_maxScale;

            // Caluclate roll angle
            var forwardToGravRej = Vector3D.Normalize(Vector3D.Reject(gravity, worldTrans.Forward));
            double rollDot = Vector3.Dot(forwardToGravRej, worldTrans.Left);
            double rollAngle = -(Math.Acos(rollDot) - MathHelper.PiOver2);
            if (gravity.Dot(worldTrans.Up) >= 0)
                rollAngle = Math.PI - rollAngle;

            // Calculate pitch angle.
            double dot = gravity.Dot(worldTrans.Forward);
            double pitchAngle = (Math.Acos(dot) - MathHelper.PiOver2);

            DrawHorizon(frame, screenForward2D, rollAngle);
            DrawLadder(frame, gravity, worldTrans, pitchAngle, horizonForward, rollAngle);

            if (m_tickCounter % 1000 == 0)
                m_nearestPlanet = MyGamePruningStructure.GetClosestPlanet(worldTrans.Translation);

            if (m_nearestPlanet != null)
            {
                int radarAltitude = DrawAltitudeWarning(frame, worldTrans, m_nearestPlanet);
                m_lastSeaLevelAlt = DrawAltimeter(frame, worldTrans, m_nearestPlanet, radarAltitude, m_textBoxSize);
                m_lastRadarAlt = radarAltitude;
            }

            var velocity = m_grid.Physics.LinearVelocity;
            DrawPullUpWarning(frame, velocity, worldTrans, rollAngle);

            var velTbDraw = m_halfSize + new Vector2(-205, 80) * m_maxScale;
            frame = DrawSpeedIndicator(frame, velTbDraw, m_textBoxSize, velocity);

            DrawVelocityVector(frame, velocity, worldTrans);
            DrawBoreSight(frame);
        }

        private void DrawHorizon(MySpriteDrawFrame frame, Vector2 screenForward2D, double rollAngle)
        {
            var size = new Vector2(m_screenDiag);
            var drawPosGround = new Vector2(0, m_screenDiag * .5f);
            drawPosGround.Rotate(rollAngle);

            var bgSprite = new MySprite(SpriteType.TEXTURE, MyTextSurfaceHelper.DEFAULT_BG_TEXTURE, m_halfSize + drawPosGround + screenForward2D, size, new Color(m_foregroundColor, .5f), rotation: (float)rollAngle);
            frame.Add(bgSprite);

            bgSprite.Position = m_halfSize - drawPosGround + screenForward2D;
            frame.Add(bgSprite);

            drawPosGround = new Vector2(0, m_screenDiag * 1.5f);
            drawPosGround.Rotate(rollAngle);
            bgSprite.Position = m_halfSize + drawPosGround + screenForward2D;
            frame.Add(bgSprite);

            bgSprite.Position = m_halfSize - drawPosGround + screenForward2D;
            frame.Add(bgSprite);

            var horizonLine = new MySprite(SpriteType.TEXTURE, MyTextSurfaceHelper.BLANK_TEXTURE, m_halfSize + screenForward2D, new Vector2(m_screenDiag, 3f * m_maxScale),
                m_foregroundColor, rotation: (float)rollAngle);
            frame.Add(horizonLine);
        }

        private void DrawLadder(MySpriteDrawFrame frame, Vector3 gravity, MatrixD worldTrans, double pitchAngle, Vector3D horizonForward, double rollAngle)
        {
            double closestFullAngle = (pitchAngle / ANGLE_STEP); // 0.174533 is 10 deg
            int roundedClosestFullAngle = (int)Math.Round(closestFullAngle);


            for (int i = roundedClosestFullAngle - 5; i <= roundedClosestFullAngle + 5; i++)
            {
                if (i == 0)
                    continue;

                var rotMat = MatrixD.CreateRotationX(i * ANGLE_STEP);
                var rotMatWorld = rotMat * MatrixD.CreateWorld(worldTrans.Translation, horizonForward, -(Vector3D)gravity);

                var screenLadder = Vector3D.Reject(rotMatWorld.Forward, worldTrans.Forward);
                screenLadder = Vector3D.TransformNormal(screenLadder, MatrixD.Invert(worldTrans));
                var screenLadder2D = new Vector2((float)screenLadder.X, -(float)screenLadder.Y) * HUD_SCALING * m_maxScale;

                var textureLadder = i * ANGLE_STEP < 0 ? "AH_GravityHudNegativeDegrees" : "AH_GravityHudPositiveDegrees";

                var ladderStep = new MySprite(SpriteType.TEXTURE, textureLadder, m_halfSize + screenLadder2D, m_ladderStepSize,
                    m_foregroundColor, rotation: (float)rollAngle);
                frame.Add(ladderStep);

                var fontSizeLadder = m_fontScale * LADDER_TEXT_SIZE_MULTIPLIER;

                int angleStepDeg = Math.Abs(i * 5);
                var stringToDraw = i > 18 ? (36 * 5 - i * 5).ToString() : angleStepDeg.ToString();

                var textOffset = new Vector2(-m_ladderStepSize.X * 0.55f, 0f);
                textOffset.Rotate(rollAngle);

                var angleTextLeft = MySprite.CreateText(stringToDraw, m_fontId, m_foregroundColor, fontSizeLadder, TextAlignment.RIGHT);
                angleTextLeft.Position = m_halfSize + screenLadder2D + textOffset - m_ladderStepTextOffset;
                frame.Add(angleTextLeft);

                textOffset = new Vector2(m_ladderStepSize.X * 0.55f, 0f);
                textOffset.Rotate(rollAngle);

                var angleTextRight = MySprite.CreateText(stringToDraw, m_fontId, m_foregroundColor, fontSizeLadder, TextAlignment.LEFT);
                angleTextRight.Position = m_halfSize + screenLadder2D + textOffset - m_ladderStepTextOffset;
                frame.Add(angleTextRight);
            }
        }

        private int DrawAltitudeWarning(MySpriteDrawFrame frame, MatrixD worldTrans, MyPlanet nearestPlanet)
        {
            var heightOfGrid = m_grid.PositionComp.LocalAABB.Height;
            var testWarningAlt = 100 + heightOfGrid;

            Vector3D closestPoint = nearestPlanet.GetClosestSurfacePointGlobal(worldTrans.Translation);
            int radarAltitude = (int)Vector3D.Distance(closestPoint, worldTrans.Translation);

            // show when crossing down
            if (m_lastRadarAlt >= testWarningAlt && radarAltitude < testWarningAlt)
            {
                m_showAltWarning = true;
                m_altWarningShownAt = m_tickCounter;
            }

            if (m_tickCounter - m_altWarningShownAt > ALTITUDE_WARNING_TIME_THRESHOLD)
                m_showAltWarning = false;

            if (m_showAltWarning)
            {
                var text = MyTexts.Get(MySpaceTexts.DisplayName_TSS_ArtificialHorizon_AltitudeWarning);
                var sizePxRadarAlt = m_surface.MeasureStringInPixels(text, m_fontId, m_fontScale);
                var radarAltitudeWarning = MySprite.CreateText(text.ToString(), m_fontId, m_foregroundColor, m_fontScale, TextAlignment.LEFT);
                radarAltitudeWarning.Position = m_halfSize + new Vector2(0, 100) - sizePxRadarAlt * 0.5f;
                frame.Add(radarAltitudeWarning);
            }

            return radarAltitude;
        }

        private double DrawAltimeter(MySpriteDrawFrame frame, MatrixD worldTrans, MyPlanet nearestPlanet, int radarAltitude, Vector2 textBoxSize)
        {
            var seaLevelAltitude = Vector3D.Distance(nearestPlanet.PositionComp.GetPosition(), worldTrans.Translation);
            seaLevelAltitude -= nearestPlanet.AverageRadius; // substract radius to get sea level

            var radarAltString = radarAltitude < RADAR_ALTITUDE_THRESHOLD ? radarAltitude.ToString() : ((int)seaLevelAltitude).ToString();
            var radarAltDrawPos = m_halfSize + new Vector2(115, 80) * m_maxScale;

            AddTextBox(frame, radarAltDrawPos + textBoxSize * 0.5f, textBoxSize, radarAltString, m_fontId, m_fontScale,
                m_foregroundColor, m_foregroundColor, "AH_TextBox", m_textOffsetInsideBox.X);

            if (radarAltitude < RADAR_ALTITUDE_THRESHOLD)
            {
                var radarSprite = MySprite.CreateText("R", m_fontId, m_foregroundColor, m_fontScale, TextAlignment.LEFT);
                var pos = radarAltDrawPos + textBoxSize * 0.5f;
                radarSprite.Position = pos + new Vector2(textBoxSize.X, -textBoxSize.Y) * 0.5f + m_textOffsetInsideBox;
                frame.Add(radarSprite);
            }

            var diffPerSec = (seaLevelAltitude - m_lastSeaLevelAlt) * 30;
            AddTextBox(frame, radarAltDrawPos + new Vector2(textBoxSize.X * 0.5f, -textBoxSize.Y * 0.5f), textBoxSize, ((int)diffPerSec).ToString(), m_fontId, m_fontScale, m_foregroundColor, m_foregroundColor, textOffset: m_textOffsetInsideBox.X);

            return seaLevelAltitude;
        }

        private void DrawPullUpWarning(MySpriteDrawFrame frame, Vector3 velocity, MatrixD worldTrans, double rollAngle)
        {
            var velocityTest = (m_grid.Physics.Mass / 16000f) * velocity;
            IHitInfo hitInfo;
            MyAPIGateway.Physics.CastRay(worldTrans.Translation, worldTrans.Translation + velocityTest, out hitInfo, 14);
            if (hitInfo != null && m_tickCounter >= 0 && m_tickCounter % 100 > 10)
            {
                var pullUpSymbol = new MySprite(SpriteType.TEXTURE, "AH_PullUp", m_halfSize, new Vector2(150, 180), m_foregroundColor,
                    rotation: (float)rollAngle);
                frame.Add(pullUpSymbol);
            }
        }

        private void DrawVelocityVector(MySpriteDrawFrame frame, Vector3 velocity, MatrixD worldTrans)
        {
            var dotVelForw = Vector3.Dot(velocity, worldTrans.Forward);
            // do not show if velocity vector points backwards
            if (dotVelForw >= -0.1f)
            {
                var vSq = velocity.LengthSquared();
                velocity.Normalize();
                var projectionVel = Vector3D.Reject(velocity, worldTrans.Forward);
                projectionVel = Vector3D.TransformNormal(projectionVel, MatrixD.Invert(worldTrans));
                var projectionVel2D = new Vector2((float)projectionVel.X, -(float)projectionVel.Y) * HUD_SCALING * m_maxScale;
                if (vSq < 9)
                    projectionVel2D = new Vector2(0, 0);

                var projectionVelDraw = new MySprite(SpriteType.TEXTURE, "AH_VelocityVector", m_halfSize + projectionVel2D, new Vector2(50, 50) * m_maxScale,
                    m_foregroundColor, rotation: (float)0);
                frame.Add(projectionVelDraw);
            }
        }

        private void DrawBoreSight(MySpriteDrawFrame frame)
        {
            var boreSight = new MySprite(SpriteType.TEXTURE, "AH_BoreSight", m_size * 0.5f + new Vector2(0, 19) * m_maxScale, new Vector2(50, 50) * m_maxScale,
                m_foregroundColor, rotation: (float)-Math.PI * 0.5f);

            frame.Add(boreSight);
        }
        #endregion

        #region Space Display
        private void DrawSpaceDisplay(MySpriteDrawFrame frame, MatrixD worldTrans)
        {
            AddBackground(frame, new Color(m_backgroundColor, .66f));

            var velocity = m_grid.Physics.LinearVelocity;

            DrawVelocityVector(frame, velocity, worldTrans);
            DrawBoreSight(frame);

            var velTbDraw = m_halfSize + new Vector2(-205, 80) * m_maxScale;
            var velBarMax = m_halfSize + new Vector2(205, 80) * m_maxScale;

            DrawSpeedIndicator(frame, velTbDraw, m_textBoxSize, velocity);

            var barBgColor = new Color(m_backgroundColor, 0.1f);
            var maxV = Math.Max(Math.Max(MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed, MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed), 1f);
            var percentage = velocity.Length() / maxV;

            var barSize = new Vector2(velBarMax.X - velTbDraw.X - m_textBoxSize.X - m_textOffsetInsideBox.X, m_textBoxSize.Y);
            AddProgressBar(frame, velTbDraw + new Vector2(barSize.X * 0.5f + m_textBoxSize.X + m_textOffsetInsideBox.X, m_textBoxSize.Y / 2f), barSize, percentage, barBgColor, m_foregroundColor);
        }
        #endregion

        private MySpriteDrawFrame DrawSpeedIndicator(MySpriteDrawFrame frame, Vector2 drawPos, Vector2 textBoxSize, Vector3 velocity)
        {
            var velLen = (int)velocity.Length();
            AddTextBox(frame, drawPos + textBoxSize * 0.5f, textBoxSize, velLen.ToString(), m_fontId, m_fontScale,
                m_foregroundColor, m_foregroundColor, "AH_TextBox", m_textOffsetInsideBox.X);

            return frame;
        }
    }
}