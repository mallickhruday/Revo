﻿using System;
using Revo.Core.Configuration;
using Revo.EF6.DataAccess.Model;

namespace Revo.EF6.DataAccess
{
    public class EF6DataAccessConfigurationSection : IRevoConfigurationSection
    {
        public bool IsActive { get; set; }
        public bool UseAsPrimaryRepository { get; set; }
        public Type[] ConventionTypes { get; set; } = { typeof(CustomStoreConventionEx) };
        public EF6ConnectionConfiguration Connection { get; set; } = new EF6ConnectionConfiguration("EntityContext");
    }
}
