/* Copyright © 2020 Gwindalmir */
using Sandbox.ModAPI;

using System;

using VRage.Game.Components;
using VRage.ObjectBuilders;

namespace Gwindalmir.RealTimeTSS {
    public class TSSBlock : MyGameLogicComponent {
        IMyTextSurfaceProvider textSurface;

        public event Action<IMyTerminalBlock, Sandbox.ModAPI.Ingame.IMyTextSurface, int> onBlockRender;
        private ulong frameCount = 0;

        public override void Init(MyObjectBuilder_EntityBase entityBase) {
            base.Init(entityBase);

            if (MyAPIGateway.Utilities?.IsDedicated == true)
                return;
            if (Entity is IMyTextSurfaceProvider) {
                textSurface = Entity as IMyTextSurfaceProvider;
                NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.EACH_FRAME;
            }
        }

        public override void Close() {
            base.Close();
            onBlockRender = null;
        }

        public override void UpdateAfterSimulation () {
            base.UpdateAfterSimulation();
            if(frameCount++ % 2 == 0) {
                for (var surfaceIdx = 0; surfaceIdx < textSurface.SurfaceCount; surfaceIdx++) {
                    onBlockRender?.Invoke( Entity as IMyTerminalBlock, textSurface.GetSurface( surfaceIdx ), surfaceIdx);
                }
            }

            // Stop game from crashing after playing for long periods.
            if (frameCount > (ulong.MaxValue - 1000)) {
                frameCount -= (ulong.MaxValue - 1000);
            }
        }
    }
}
