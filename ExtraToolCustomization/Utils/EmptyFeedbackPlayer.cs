using Feedback;

namespace ExtraToolCustomization.Utils
{
    internal static class EmptyFeedbackPlayer
    {
        public readonly static FeedbackPlayer Instance;

        static EmptyFeedbackPlayer()
        {
            Instance = new(GameData.FeedbackDataBlock.GetBlock(1u), true)
            {
                name = "Empty_4115",
                m_effectPlayers = null,
                m_audioPlayers = null,
                hasEffects = false,
                hasAudio = false
            };
        }
    }
}
