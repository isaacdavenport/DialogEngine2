//  Confidential Source Code Property Toys2Life LLC Colorado 2017
//  www.toys2life.org

using System.ComponentModel;

namespace DialogGenerator.Utilities
{
    /// <summary>
    /// Provides access to functionality that is common
    /// across all sound players.
    /// </summary>
    /// <seealso cref="ISpectrumPlayer"/>
    public interface ISoundPlayer : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets whether the sound player is currently playing audio.
        /// </summary>
        bool IsPlaying { get; }
        /// <summary>
        /// Gets whether the sound player is currently recording audio.
        /// </summary>
        bool IsRecording { get; }
    }
}
