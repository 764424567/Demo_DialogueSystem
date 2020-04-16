// Copyright (c) Pixel Crushers. All rights reserved.

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Quest state is a bit-flag enum that indicates the state of a quest. 
    /// This enum is used by the QuestLog class.
    /// </summary>
    [System.Flags]
    public enum QuestState
    {

        /// <summary>
        /// Quest is unassigned
        /// </summary>
        Unassigned = 0x1,

        /// <summary>
        /// Quest is active (assigned but not completed yet)
        /// </summary>
        Active = 0x2,

        /// <summary>
        /// Quest was completed successfully; corresponds to "success" or "done"
        /// </summary>
        Success = 0x4,

        /// <summary>
        /// Quest was completed in failure
        /// </summary>
        Failure = 0x8,

        /// <summary>
        /// Quest was abandoned
        /// </summary>
        Abandoned = 0x10
    }

}
