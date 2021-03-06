﻿using Revo.DataAccess.Entities;
using Revo.Domain.ReadModel;

namespace Revo.Examples.HelloAspNet.Bootstrap.ReadSide.Model
{
    [TablePrefix(NamespacePrefix = "REX", ColumnPrefix = "TOD")]
    public class TodoReadModel : EntityReadModel
    {
        public string Title { get; set; }
    }
}