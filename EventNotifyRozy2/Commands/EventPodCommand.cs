﻿using CommandSystem;
using Exiled.Permissions.Extensions;
using HarmonyLib;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Exiled.API.Features;
using RemoteAdmin;
using System.Net.Http;
using Exiled.Events.Commands.PluginManager;
using System.Collections;

namespace EventNotifyRozy2
{

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class EventPodCommand : ICommand, IUsageProvider
    {
        public bool SanitizeResponse => false;
        private readonly EventPugin plugin;
        public string Command => "Eventpod";
        public string[] Aliases { get; } = { "Evpod" };
        public string Description => "Start event preparation";
        public string[] Usage => new string[] { "RP", "Name of Event" };
        private static readonly HttpClient httpClient = new HttpClient();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!EventPugin.plugin.Config.EventCodeActive)
            {
                response = "The command is unavailable as it is disabled in the configuration.";
                return false;
            }
            if (!((CommandSender)sender).CheckPermission("rozy.command"))
            {
                response = "You do not have the 'rozy.command' permission.";
                return false;
            }

            EventPugin.EventMaster = ((CommandSender)sender).Nickname;
            EventPugin.EventMasterGroup = EventPugin.GetUserGroup((CommandSender)sender);

            if (arguments.Array.Length > 1)
            {
                EventPugin.EventRP = arguments.Array[1];
            }

            if (arguments.Array.Length > 2)
            {
                EventPugin.EventName = "";
                for (int i = 2; i < arguments.Array.Length; i++)
                {
                    EventPugin.EventName = EventPugin.EventName + arguments.Array[i] + " ";
                }
            }

            if (!EventPugin.EventPreparation)
            {
                EventPugin.time = new TimeSpan(0, 0, 0);
                EventPugin.EventPreparation = true;
                EventPugin.hintCoroutine = Timing.RunCoroutine(EventPugin.HintCoroutine());
                response = "Preparation has started.";
                string webhookUrl = EventPugin.plugin.Config.WebhookUrl;
                string eventName = EventPugin.EventName.Trim();
                string eventRP = EventPugin.EventRP;
                string eventMaster = EventPugin.EventMaster;
                string eventMasterGroup = EventPugin.EventMasterGroup;
                int playerCount = Player.List.Count();
                string message = $"\n\n*Preparation for the event has started on the server* **{eventName}** \n*with RP level* **{eventRP}**.\n*Host:* **{eventMaster}** (Role: {eventMasterGroup})\n*Number of players at the start of preparation:* **{playerCount}**.";
                SendWebhookMessage(webhookUrl, message).ConfigureAwait(false);
            }
            else
            {
                response = "Preparation is already in progress.";
            }

            return true;
        }

        private static async Task SendWebhookMessage(string webhookUrl, string message)
        {
            var payload = new
            {
                content = message
            };
            var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(webhookUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"Failed to send webhook message: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Exception caught while sending webhook message: {ex}");
            }
        }
    }
}
