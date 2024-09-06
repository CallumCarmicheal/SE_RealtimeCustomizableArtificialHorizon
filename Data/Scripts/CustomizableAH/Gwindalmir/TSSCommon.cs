/* Copyright © 2020 Gwindalmir */
using Sandbox.Game.Components;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;

using System;
using System.Collections.Generic;
using System.Text;

using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;

using VRageMath;

using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace Gwindalmir.RealTimeTSS {
    public abstract class TSSCommon : MyTSSCommon {



        public static float ASPECT_RATIO = 3f;

        public static float DecorationSizeScale = 0.25f;

        public static float TEXT_RATIO = 0.25f;


        private bool isRunning = false;

        private TSSBlock terminalTSSBlock;

        private ulong I0lI0IlIi = 0UL;

        private bool l0lI1I0Il = true;

        private List<MySprite> o0II0IlIi = new List<MySprite>();

        private Vector2 i0IIOIiIo = Vector2.One;

        private bool O1OIiIIIo = false;

        protected Vector2 m_innerSize;

        protected Vector2 m_decorationSize;

        protected StringBuilder m_sb = new StringBuilder();

        protected float m_firstLine;

        protected float m_secondLine;

        protected byte m_updateRateDivisor = 2;

        protected MatrixD m_lastBlockMatrix;


        public TSSCommon(Sandbox.ModAPI.Ingame.IMyTextSurface surface, VRage.Game.ModAPI.Ingame.IMyCubeBlock block, Vector2 size) : base(surface, block, size) {
            this.m_innerSize = new Vector2(TSSCommon.ASPECT_RATIO, 1f);
            MyTextSurfaceScriptBase.FitRect(this.m_surface.SurfaceSize, ref this.m_innerSize);
            this.m_decorationSize = new Vector2(0.012f * this.m_innerSize.X, TSSCommon.DecorationSizeScale * this.m_innerSize.Y);

            this.m_sb.Clear();
            this.m_sb.Append("00.000");

            Vector2 stringSize = this.m_surface.MeasureStringInPixels(this.m_sb, this.m_fontId, 1f);

            float lines = TSSCommon.TEXT_RATIO * this.m_innerSize.Y / stringSize.Y;

            this.m_fontScale = Math.Min(this.m_innerSize.X * 0.72f / stringSize.X, lines);
            this.m_firstLine = this.m_halfSize.Y - this.m_decorationSize.Y * 0.55f;
            this.m_secondLine = this.m_halfSize.Y + this.m_decorationSize.Y * 0.55f;

            bool isWidescreen = this.m_surface.SurfaceSize.X > this.m_surface.SurfaceSize.Y;
            if (isWidescreen) {
                this.i0IIOIiIo = new Vector2(1f, this.m_surface.SurfaceSize.Y / this.m_surface.SurfaceSize.X);
            } else {
                this.i0IIOIiIo = new Vector2(this.m_surface.SurfaceSize.X / this.m_surface.SurfaceSize.Y, 1f);
            }
            this.IO1lIIoIO();
        }

        public abstract void Draw(MySpriteDrawFrame lOOloIlIi);

        public override ScriptUpdate NeedsUpdate {
            get {
                return (this.terminalTSSBlock == null) ? ScriptUpdate.Update10 : ScriptUpdate.Update10000;
            }
        }

        public override void Run() {
            try {
                using (MySpriteDrawFrame lOOloIlIi = base.Surface.DrawFrame()) {
                    this.Draw(lOOloIlIi);
                }
            } catch (Exception oOOliI1Ii) {
                bool flag = !this.O1OIiIIIo;
                if (flag) {
                    Logger.Instance.LogException(oOOliI1Ii, false);
                }
                this.O1OIiIIIo = true;
            }
        }

        public virtual List<MySprite> RunSpecial() {
            ulong i0lI0IlIi = this.I0lI0IlIi;
            this.I0lI0IlIi = i0lI0IlIi + 1UL;
            bool flag = i0lI0IlIi % (ulong)this.m_updateRateDivisor == 0UL;
            if (flag) {
                this.m_backgroundColor = this.m_surface.ScriptBackgroundColor;
                this.m_foregroundColor = this.m_surface.ScriptForegroundColor;
                this.m_lastBlockMatrix = this.m_block.WorldMatrix;
                try {
                    using (MySpriteDrawFrame spriteFrame = new MySpriteDrawFrame(new Action<MySpriteDrawFrame>(this.iOolII0Ii))) {
                        this.Draw(spriteFrame);
                    }
                    bool flag2 = this.l0lI1I0Il;
                    if (flag2) {
                        return this.o0II0IlIi;
                    }
                } catch (Exception oOOliI1Ii) {
                    bool flag3 = !this.O1OIiIIIo;
                    if (flag3) {
                        Logger.Instance.LogException(oOOliI1Ii, false);
                    }
                    this.O1OIiIIIo = true;
                }
            }
            return null;
        }

        private void iOolII0Ii(MySpriteDrawFrame lOOloIlIi) {
            try {
                MySpriteCollection OIolOIiIi = lOOloIlIi.ToCollection();
                MySprite[] sprites = OIolOIiIi.Sprites;
                int? num = (sprites != null) ? new int?(sprites.Length) : null;
                int count = this.o0II0IlIi.Count;
                bool flag = !(num.GetValueOrDefault() == count & num != null);
                if (flag) {
                    this.l0lI1I0Il = true;
                } else {
                    for (int II1llIiIO = 0; II1llIiIO < OIolOIiIi.Sprites.Length; II1llIiIO++) {
                        bool flag2 = !OIolOIiIi.Sprites[II1llIiIO].Equals(this.o0II0IlIi[II1llIiIO]);
                        if (flag2) {
                            this.l0lI1I0Il = true;
                            return;
                        }
                    }
                    this.l0lI1I0Il = false;
                }
            } finally {
                bool flag3 = this.l0lI1I0Il;
                if (flag3) {
                    this.o0II0IlIi.Clear();
                    lOOloIlIi.AddToList(this.o0II0IlIi);
                }
            }
        }

        private void IO1lIIoIO() {
            if (!this.isRunning) {
                IMyUtilities utilities = MyAPIGateway.Utilities;

                if (utilities != null && utilities.IsDedicated) {
                    this.isRunning = true;
                } else {
                    this.terminalTSSBlock = (this.m_block as Sandbox.ModAPI.IMyTerminalBlock).GameLogic.GetAs<TSSBlock>();
                    bool tbDoesntExist = this.terminalTSSBlock == null 
                        && !(this.m_block as Sandbox.ModAPI.IMyTerminalBlock).MarkedForClose 
                        && !(this.m_block as Sandbox.ModAPI.IMyTerminalBlock).Closed;
                    if (tbDoesntExist) {
                        this.terminalTSSBlock = new TSSBlock();
                        (this.m_block as Sandbox.ModAPI.IMyTerminalBlock).GameLogic.Container.Add<TSSBlock>(this.terminalTSSBlock);
                        this.terminalTSSBlock.Init(null);
                        (this.m_block as Sandbox.ModAPI.IMyTerminalBlock).OnMarkForClose += delegate (VRage.ModAPI.IMyEntity lIillI0II) {
                            this.terminalTSSBlock.onBlockRender -= this.terminalTSSBlock_BlockRender;
                            this.terminalTSSBlock.Close();
                            lIillI0II.Components.Remove<TSSBlock>();
                        };
                    }
                    if (this.terminalTSSBlock != null) {
                        this.terminalTSSBlock.onBlockRender += this.terminalTSSBlock_BlockRender;
                        this.isRunning = true;
                    }
                }
            }
        }

        private void terminalTSSBlock_BlockRender(Sandbox.ModAPI.IMyTerminalBlock renderBlock, Sandbox.ModAPI.Ingame.IMyTextSurface renderSurface, int surfaceIdx) {
            bool flag;
            if (renderBlock != null && renderSurface != null && this.m_block != null && this.m_surface != null) {
                long? num = (renderBlock != null) ? new long?(renderBlock.EntityId) : null;
                VRage.Game.ModAPI.Ingame.IMyCubeBlock block = this.m_block;

                long? num2 = (block != null) ? new long?(block.EntityId) : null;
                if (num.GetValueOrDefault() == num2.GetValueOrDefault() & num != null == (num2 != null)) {
                    string a = (renderSurface != null) ? renderSurface.Name : null;
                    Sandbox.ModAPI.Ingame.IMyTextSurface surface = this.m_surface;
                    if (!(a != ((surface != null) ? surface.Name : null))) {
                        string a2 = (renderSurface != null) ? renderSurface.Script : null;
                        Sandbox.ModAPI.Ingame.IMyTextSurface surface2 = this.m_surface;
                        flag = (a2 != ((surface2 != null) ? surface2.Script : null));
                        goto IL_CB;
                    }
                }
            }
            flag = true;
        IL_CB:
            bool flag2 = flag;
            if (!flag2) {
                try {
                    List<MySprite> llOlOI0Il = this.RunSpecial();
                    bool flag3 = llOlOI0Il != null && llOlOI0Il.Count > 0;
                    if (flag3) {
                        MyRenderComponentScreenAreas myRenderComponentScreenAreas = renderBlock.Render as MyRenderComponentScreenAreas;
                        if (myRenderComponentScreenAreas != null) {
                            myRenderComponentScreenAreas.RenderSpritesToTexture(surfaceIdx, llOlOI0Il, new Vector2I((int)this.m_size.X, (int)this.m_size.Y), this.i0IIOIiIo, this.m_backgroundColor, 0);
                        }
                    }
                } catch (Exception oOOliI1Ii) {
                    bool flag4 = !this.O1OIiIIIo;
                    if (flag4) {
                        Logger.Instance.LogException(oOOliI1Ii, false);
                    }
                    this.O1OIiIIIo = true;
                }
            }
        }

        public override void Dispose() {
            base.Dispose();
            bool flag = this.terminalTSSBlock != null;
            if (flag) {
                this.terminalTSSBlock.onBlockRender -= this.terminalTSSBlock_BlockRender;
            }
        }


    }
}
