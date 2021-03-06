﻿using AutoMapper;
using Revo.Core.IO;
using Revo.Extensions.History.ChangeTracking.Model;

namespace Revo.Extensions.History.ChangeTracking
{
    public class HistoryAutoMapperProfile : Profile
    {
        public HistoryAutoMapperProfile(ITrackedChangeRecordConverter trackedChangeRecordConverter)
        {
            CreateMap<TrackedChangeRecord, TrackedChange>()
                .ProjectUsing(x => trackedChangeRecordConverter.FromRecord(x));
        }
    }
}
