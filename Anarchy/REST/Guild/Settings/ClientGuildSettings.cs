﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace Discord
{
    public class ClientGuildSettings : Controllable
    {
        [JsonProperty("guild_id")]
        private readonly ulong _guildId;

        public MinimalGuild Guild
        {
            get
            {
                return new MinimalGuild(_guildId).SetClient(Client);
            }
        }


        [JsonProperty("muted")]
        public bool Muted { get; private set; }


        [JsonProperty("message_notifications")]
        public ClientNotificationLevel Notifications { get; private set; }


        [JsonProperty("supress_everyone")]
        public bool SupressEveryone { get; private set; }


        [JsonProperty("supress_roles")]
        public bool SupressRoles { get; private set; }


        [JsonProperty("mobile_push")]
        public bool MobilePushNotifications { get; private set; }


        [JsonProperty("channel_overrides")]
        public IReadOnlyList<DiscordChannelSettings> ChannelOverrides { get; private set; }
    }
}
