using System;

namespace splitavi
{
    /// <summary>
    /// A recording is the metadata associated with range of frames that together make up a recording. A recording is
    /// a "clip" or "scene". I.e. the video recorded from the moment the user presses start and then stop. A recording
    /// is a subset of a video.
    /// </summary>
    public class Recording
    {
        /// <summary>
        /// The frame number this recording starts on.
        /// </summary>
        public int Frame { get; set; }
        
        /// <summary>
        /// The offset into the video where this recording starts. 
        /// </summary>
        public TimeSpan StartOffset { get; set; }
        
        /// <summary>
        /// The offset into the video where this recording ends.
        /// </summary>
        public TimeSpan EndOffset { get; set; }
        
        /// <summary>
        /// The date and time this recording started, in UTC.
        /// </summary>
        public DateTime RecordingDateTime { get; set; }

        public override string ToString()
        {
            return $"{Frame} {StartOffset} {EndOffset} {RecordingDateTime}";
        }
    }
}