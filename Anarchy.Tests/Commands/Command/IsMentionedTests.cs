namespace Discord.Commands.Command
{
    [TestClass]
    public class IsMentionedTests
    {
        public bool AllowMention { get; set; } = true;

        [TestMethod]
        public void CheckForMention()
        {
            IReadOnlyList<DiscordUser> mentionedFirst = new List<DiscordUser>()
            {
                Globals.Client.User, 
                Globals.Client.GetUser(367766308290297857), // random id
                Globals.Client.GetUser(558312233147432960)  // another random id
            };

            IReadOnlyList<DiscordUser> notMentioned = new List<DiscordUser>()
            {
                Globals.Client.GetUser(367766308290297857), // random id
                Globals.Client.GetUser(558312233147432960)  // another random id
            };

            IReadOnlyList<DiscordUser> mentionedNotFirst = new List<DiscordUser>()
            {
                Globals.Client.GetUser(367766308290297857), // random id
                Globals.Client.User,
                Globals.Client.GetUser(558312233147432960)  // another random id
            };

            // should only pass if we are mentioned first
            Assert.IsTrue(IsMentioned(mentionedFirst));
            Assert.IsFalse(IsMentioned(notMentioned));
            Assert.IsFalse(IsMentioned(mentionedNotFirst));
        }

        private bool IsMentioned(IReadOnlyList<DiscordUser> mentions)
        {
            if (!AllowMention)
            {
                return false;
            }
            if (mentions.Count == 0)
            {
                return false;
            }
            if (mentions.FirstOrDefault().Id == Globals.Client.User.Id)
            {
                return true;
            }
            return false;
        }
    }
}
