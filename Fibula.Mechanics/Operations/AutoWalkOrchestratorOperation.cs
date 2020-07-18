﻿// -----------------------------------------------------------------
// <copyright file="AutoWalkOrchestratorOperation.cs" company="2Dudes">
// Copyright (c) | Jose L. Nunez de Caceres et al.
// https://linkedin.com/in/nunezdecaceres
//
// All Rights Reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------

namespace Fibula.Mechanics.Operations
{
    using System;
    using System.Linq;
    using Fibula.Common.Contracts.Enumerations;
    using Fibula.Common.Contracts.Extensions;
    using Fibula.Common.Contracts.Structs;
    using Fibula.Common.Utilities;
    using Fibula.Common.Utilities.Pathfinding;
    using Fibula.Communications.Packets.Outgoing;
    using Fibula.Creatures.Contracts.Abstractions;
    using Fibula.Creatures.Contracts.Constants;
    using Fibula.Mechanics.Contracts.Abstractions;
    using Fibula.Mechanics.Contracts.Enumerations;
    using Fibula.Mechanics.Contracts.Extensions;
    using Fibula.Mechanics.Notifications;

    /// <summary>
    /// Class that represents an operation that orchestrates auto walk operations.
    /// </summary>
    public class AutoWalkOrchestratorOperation : Operation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoWalkOrchestratorOperation"/> class.
        /// </summary>
        /// <param name="creature">The creature that is auto walking.</param>
        public AutoWalkOrchestratorOperation(ICreature creature)
            : base(creature?.Id ?? 0)
        {
            creature.ThrowIfNull(nameof(creature));

            this.Creature = creature;
        }

        /// <summary>
        /// Gets the type of exhaustion that this operation produces.
        /// </summary>
        public override ExhaustionType ExhaustionType => ExhaustionType.None;

        /// <summary>
        /// Gets or sets the exhaustion cost time of this operation.
        /// </summary>
        public override TimeSpan ExhaustionCost { get; protected set; }

        /// <summary>
        /// Gets the combatant that is attacking on this operation.
        /// </summary>
        public ICreature Creature { get; }

        /// <summary>
        /// Executes the operation's logic.
        /// </summary>
        /// <param name="context">A reference to the operation context.</param>
        protected override void Execute(IOperationContext context)
        {
            if (this.Creature == null || this.Creature.IsDead)
            {
                return;
            }

            var resultingState = this.Creature.WalkPlan.UpdateState(this.Creature.Location);

            if (this.Creature.WalkPlan.IsInTerminalState)
            {
                return;
            }

            // Recalculate the route if necessary:
            if (resultingState == WalkPlanState.NeedsToRecalculate)
            {
                var (result, _, directions) = context.PathFinder.FindBetween(this.Creature.Location, this.Creature.WalkPlan.DetermineTargetLocation(), this.Creature, targetDistance: this.Creature.WalkPlan.AtGoalDistanceFromLocation);

                if (result == SearchState.Failed && !directions.Any())
                {
                    // No way found.
                    this.DispatchTextNotification(context, OperationMessage.ThereIsNoWay);

                    if (this.Creature is ICombatant combatant)
                    {
                        context.CombatApi.SetCombatantModes(combatant, combatant.FightMode, ChaseMode.Stand, false /*combatant.HasSafetyOn*/);
                    }
                }
                else
                {
                    this.Creature.WalkPlan.RecalculateWaypoints(this.Creature.Location, directions);

                    // Repeat immediately.
                    this.RepeatAfter = TimeSpan.Zero;
                }

                return;
            }

            if (this.Creature.WalkPlan.Waypoints.Count > 0)
            {
                // Pop the current waypoint.
                this.Creature.WalkPlan.Waypoints.RemoveFirst();

                // If even after recalculating we're left without waypoints, this is the last move and we are done.
                if (this.Creature.WalkPlan.Waypoints.Count == 0)
                {
                    if (this.Creature is IPlayer player)
                    {
                        new GenericNotification(
                            () => player.YieldSingleItem(),
                            new PlayerCancelWalkPacket(player.Direction.GetClientSafeDirection()))
                        .Send(new NotificationContext(context.Logger, context.MapDescriptor, context.CreatureFinder));
                    }

                    return;
                }
            }

            var nextLocation = this.Creature.WalkPlan.Waypoints.First.Value;
            var scheduleDelay = TimeSpan.Zero;

            var autoWalkOp = new MovementOperation(
                    this.Creature.Id,
                    CreatureConstants.CreatureThingId,
                    this.Creature.Location,
                    byte.MaxValue,
                    this.Creature.Id,
                    nextLocation,
                    this.Creature.Id,
                    amount: 1);

            // Add delay from current exhaustion of the requestor, if any.
            if (this.Creature is ICreatureWithExhaustion creatureWithExhaustion)
            {
                // The scheduling delay becomes any cooldown debt for this operation.
                scheduleDelay = creatureWithExhaustion.CalculateRemainingCooldownTime(autoWalkOp.ExhaustionType, context.Scheduler.CurrentTime);
            }

            // Schedule the actual walk operation.
            context.Scheduler.ScheduleEvent(autoWalkOp, scheduleDelay);

            this.RepeatAfter = this.Creature.CalculateStepDuration(context.Map.GetTileAt(this.Creature.Location));
        }
    }
}
