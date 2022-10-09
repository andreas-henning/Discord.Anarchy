using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Discord.Gateway;

namespace Discord.Commands
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        public string Prefix { get; private set; }
        public bool AllowMention { get; private set; }
        public Dictionary<string, DiscordCommand> Commands { get; private set; }

        internal CommandHandler(string prefix, DiscordSocketClient client, bool allowMention)
        {
            _client = client;

            Prefix = prefix;

            AllowMention = allowMention;

            client.OnMessageReceived += Client_OnMessageReceived;

            Assembly executable = Assembly.GetEntryAssembly();

            Commands = new Dictionary<string, DiscordCommand>();
            foreach (var type in executable.GetTypes())
            {
                if (typeof(CommandBase).IsAssignableFrom(type) && TryGetAttribute(type.GetCustomAttributes(), out CommandAttribute attr))
                    Commands.Add(attr.Name, new DiscordCommand(type, attr));
            }
        }

        private bool Mentioned(IReadOnlyList<DiscordUser> mentions)
        {
            if (!AllowMention)
            {
                return false;
            }
            if (mentions.Count == 0)
            {
                return false;
            }
            if (mentions.FirstOrDefault().Id == _client.User.Id)
            {
                return true;
            }
            return false;
        }

        private void Client_OnMessageReceived(DiscordSocketClient client, MessageEventArgs args)
        {
            bool isMentioned = Mentioned(args.Message.Mentions);
            // message must start with the prefix
            if (!args.Message.Content.StartsWith(Prefix) && !isMentioned)
            {
                return;
            }

            List<string> parts = args.Message.Content.Split(' ').ToList();
            DiscordCommand command;

            if (!Commands.TryGetValue(parts[0].Substring(Prefix.Length), out command) && (isMentioned && !Commands.TryGetValue(parts[1], out command)))
            {
                return;
            }

            parts.RemoveAt(0);

            if (isMentioned)
            {
                parts.RemoveAt(0);
            }

            CommandBase inst = (CommandBase)Activator.CreateInstance(command.Type);
            inst.Prepare(_client, args.Message);

            if (parts.Count > command.Parameters.Count)
            {
                inst.HandleError(null, null, new ArgumentNullException("Too many arguments provided"));
                return;
            }

            for (int i = 0; i < command.Parameters.Count; i++)
            {
                var param = command.Parameters[i];

                if (param.Optional)
                    continue;

                if (i < parts.Count)
                {
                    try
                    {
                        object value;

                        if (param.Property.PropertyType == typeof(string) && i == command.Parameters.Count - 1)
                            value = string.Join(" ", parts.Skip(i));
                        else if (args.Message.Guild != null && parts[i].StartsWith("<") && parts[i].EndsWith(">"))
                            value = ParseReference(param.Property.PropertyType, parts[i]);
                        else
                            value = parts[i];

                        if (!param.Property.PropertyType.IsAssignableFrom(value.GetType()))
                            value = Convert.ChangeType(value, param.Property.PropertyType);

                        param.Property.SetValue(inst, value);
                    }
                    catch (Exception ex)
                    {
                        inst.HandleError(param.Name, parts[i], ex);

                        return;
                    }
                }
                else
                {
                    inst.HandleError(param.Name, null, new ArgumentNullException("missing argument"));
                    return;
                }
            }

            inst.Execute();
        }

        // https://discord.com/developers/docs/reference#message-formatting
        private object ParseReference(Type expectedType, string reference)
        {
            string value = reference.Substring(1, reference.Length - 2);

            // Get the object's ID (always last thing in the sequence)

            MatchCollection matches = Regex.Matches(value, @"\d{18,}");
            if (matches.Count > 0)
            {
                Match match = matches[matches.Count - 1];

                if (match.Index + match.Length == value.Length)
                {
                    ulong anyId = ulong.Parse(match.Value);

                    string forSpecific = value.Substring(0, match.Index);

                    if (expectedType.IsAssignableFrom(typeof(MinimalChannel)))
                    {
                        if (forSpecific != "#")
                        {
                            throw new ArgumentException("Invalid reference type");
                        }
                        if (!expectedType.IsAssignableFrom(typeof(DiscordChannel)))
                        {
                            return new MinimalTextChannel(anyId).SetClient(_client);
                        }

                        if (_client.Config.Cache)
                            return _client.GetChannel(anyId);
                        else
                            throw new InvalidOperationException("Caching must be enabled to parse DiscordChannels");
                    }
                    else if (expectedType == typeof(DiscordRole))
                    {
                        if (!forSpecific.StartsWith("@&"))
                        {
                            throw new ArgumentException("Invalid reference type");
                        }

                        if (_client.Config.Cache)
                            return _client.GetGuildRole(anyId);
                        else
                            throw new InvalidOperationException("Caching must be enabled to parse DiscordChannels");
                        
                    }
                    else if (expectedType.IsAssignableFrom(typeof(PartialEmoji)))
                    {
                        if (!Regex.IsMatch(forSpecific, @"a?:\w+:"))
                        {
                            throw new ArgumentException("Invalid reference type");    
                        }

                        string[] split = forSpecific.Split(':');

                        bool animated = split[0] == "a";
                        string name = split[1];

                        if (expectedType != typeof(DiscordEmoji))
                        {
                            return new PartialEmoji(anyId, name, animated).SetClient(_client);
                        }

                        if (_client.Config.Cache)
                            return _client.GetGuildEmoji(anyId);
                        else
                            throw new InvalidOperationException("Caching must be enabled to parase DiscordEmojis");
                    }
                    
                    return anyId;
                }
            }

            throw new ArgumentException("Invalid reference");
        }

        internal static bool TryGetAttribute<TAttr>(IEnumerable<object> attributes, out TAttr attr) where TAttr : Attribute
        {
            foreach (var attribute in attributes)
            {
                if (attribute.GetType() == typeof(TAttr))
                {
                    attr = (TAttr)attribute;
                    return true;
                }
            }

            attr = null;
            return false;
        }
    }
}
