﻿// -----------------------------------------------------------------
// <copyright file="IAccountRepository.cs" company="2Dudes">
// Copyright (c) | Jose L. Nunez de Caceres et al.
// https://linkedin.com/in/nunezdecaceres
//
// All Rights Reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------

namespace Fibula.Data.Contracts.Abstractions
{
    using Fibula.Data.Entities.Contracts.Abstractions;

    /// <summary>
    /// Interface for an accounts repository.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity.</typeparam>
    public interface IAccountRepository<TEntity> : IRepository<TEntity>
        where TEntity : IAccountEntity
    {
    }
}
