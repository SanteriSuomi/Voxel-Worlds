using UnityEngine;

namespace Voxel.Utility.YieldInstructions
{
    public class WaitFrames : CustomYieldInstruction
    {
        private int MaxFramesUntilWait { get; }
        public int SkippedFrames { get; private set; }

        public override bool keepWaiting
        {
            get
            {
                SkippedFrames++;
                if (SkippedFrames >= MaxFramesUntilWait)
                {
                    SkippedFrames = 0;
                    return true;
                }

                return false;
            }
        }

        public WaitFrames(int maxSkippedFrames) => MaxFramesUntilWait = maxSkippedFrames;
    }
}