﻿// -----------------------------------------------------------------
// <copyright file="LookAtPacketReader.cs" company="2Dudes">
// Copyright (c) 2018 2Dudes. All rights reserved.
// Author: Jose L. Nunez de Caceres
// jlnunez89@gmail.com
// http://linkedin.com/in/jlnunez89
//
// Licensed under the MIT license.
// See LICENSE.txt file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------

namespace Fibula.Protocol.V772.PacketReaders
{
    using Fibula.Common.Contracts.Structs;
    using Fibula.Common.Utilities;
    using Fibula.Communications;
    using Fibula.Communications.Contracts.Abstractions;
    using Fibula.Communications.Packets.Incoming;
    using Serilog;

    /// <summary>
    /// Class that represents a look at packet reader for the game server.
    /// </summary>
    public sealed class LookAtPacketReader : BasePacketReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LookAtPacketReader"/> class.
        /// </summary>
        /// <param name="logger">A reference to the logger in use.</param>
        public LookAtPacketReader(ILogger logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Reads a packet from the given <see cref="INetworkMessage"/>.
        /// </summary>
        /// <param name="message">The message to read from.</param>
        /// <returns>The packet read from the message.</returns>
        public override IIncomingPacket ReadFromMessage(INetworkMessage message)
        {
            message.ThrowIfNull(nameof(message));

            return new LookAtPacket(
                location: new Location
                {
                    X = message.GetUInt16(),
                    Y = message.GetUInt16(),
                    Z = (sbyte)message.GetByte(),
                },
                thingId: message.GetUInt16(),
                stackPos: message.GetByte());
        }
    }
}
