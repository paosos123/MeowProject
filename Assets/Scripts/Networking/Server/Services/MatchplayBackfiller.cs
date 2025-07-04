using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class MatchplayBackfiller : IDisposable
{
    private CreateBackfillTicketOptions createBackfillOptions;
    private BackfillTicket localBackfillTicket;
    private bool localDataDirty;
    private int maxPlayers;
    private const int TicketCheckMs = 1000;

    private int MatchPlayerCount => localBackfillTicket?.Properties.MatchProperties.Players.Count ?? 0;

    private MatchProperties MatchProperties => localBackfillTicket.Properties.MatchProperties;
    public bool IsBackfilling { get; private set; }

    public MatchplayBackfiller(string connection, string queueName, MatchProperties matchmakerPayloadProperties, int maxPlayers)
    {
        this.maxPlayers = maxPlayers;
        BackfillTicketProperties backfillProperties = new BackfillTicketProperties(matchmakerPayloadProperties);
        localBackfillTicket = new BackfillTicket
        {
            Id = matchmakerPayloadProperties.BackfillTicketId,
            Properties = backfillProperties
        };

        createBackfillOptions = new CreateBackfillTicketOptions
        {
            Connection = connection,
            QueueName = queueName,
            Properties = backfillProperties
        };
    }

    public async Task BeginBackfilling()
    {
        if (IsBackfilling)
        {
            Debug.LogWarning("Already backfilling, no need to start another.");
            return;
        }

        Debug.Log($"Starting backfill Server: {MatchPlayerCount}/{maxPlayers}");

        if (string.IsNullOrEmpty(localBackfillTicket.Id))
        {
            localBackfillTicket.Id = await MatchmakerService.Instance.CreateBackfillTicketAsync(createBackfillOptions);
        }

        IsBackfilling = true;

        BackfillLoop();
    }



    public int RemovePlayerFromMatch(string userId)
    {
        Player playerToRemove = GetPlayerById(userId);
        if (playerToRemove == null)
        {
            Debug.LogWarning($"No user by the ID: {userId} in local backfill Data.");
            return MatchPlayerCount;
        }

        MatchProperties.Players.Remove(playerToRemove);
        GetTeamByUserId(userId).PlayerIds.Remove(userId);
        localDataDirty = true;

        return MatchPlayerCount;
    }

    public bool NeedsPlayers()
    {
        return MatchPlayerCount < maxPlayers;
    }

    public Team GetTeamByUserId(string userId)
    {
        return MatchProperties.Teams.FirstOrDefault(
            p => p.PlayerIds.Contains(userId));
    }

    private Player GetPlayerById(string userId)
    {
        return MatchProperties.Players.FirstOrDefault(
            p => p.Id.Equals(userId));
    }

    public async Task StopBackfill()
    {
        if (!IsBackfilling)
        {
            Debug.LogError("Can't stop backfilling before we start.");
            return;
        }

        await MatchmakerService.Instance.DeleteBackfillTicketAsync(localBackfillTicket.Id);
        IsBackfilling = false;
        localBackfillTicket.Id = null;
    }

    private async void BackfillLoop()
    {
        while (IsBackfilling)
        {
            if (localDataDirty)
            {
                await MatchmakerService.Instance.UpdateBackfillTicketAsync(localBackfillTicket.Id, localBackfillTicket);
                localDataDirty = false;
            }
            else
            {
                localBackfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(localBackfillTicket.Id);
            }

            if (!NeedsPlayers())
            {
                await StopBackfill();
                break;
            }

            await Task.Delay(TicketCheckMs);
        }
    }

    public void Dispose()
    {
        _ = StopBackfill();
    }
}