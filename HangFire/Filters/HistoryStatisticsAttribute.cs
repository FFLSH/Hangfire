﻿// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using HangFire.Common.Filters;
using HangFire.Common.States;
using HangFire.States;

namespace HangFire.Filters
{
    public class HistoryStatisticsAttribute : JobFilterAttribute, IStateChangingFilter
    {
        public HistoryStatisticsAttribute()
        {
            Order = 30;
        }

        public void OnStateChanging(StateChangingContext context)
        {
            using (var transaction = context.Redis.CreateTransaction())
            {
                if (context.CandidateState.StateName == SucceededState.Name)
                {
                    transaction.QueueCommand(x => x.IncrementValue(
                        String.Format("hangfire:stats:succeeded:{0}", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

                    var hourlySucceededKey = String.Format(
                        "hangfire:stats:succeeded:{0}",
                        DateTime.UtcNow.ToString("yyyy-MM-dd-HH"));
                    transaction.QueueCommand(x => x.IncrementValue(hourlySucceededKey));
                    transaction.QueueCommand(x => x.ExpireEntryIn(hourlySucceededKey, TimeSpan.FromDays(1)));
                }
                else if (context.CandidateState.StateName == FailedState.Name)
                {
                    transaction.QueueCommand(x => x.IncrementValue(
                        String.Format("hangfire:stats:failed:{0}", DateTime.UtcNow.ToString("yyyy-MM-dd"))));

                    transaction.QueueCommand(x => x.IncrementValue(
                        String.Format("hangfire:stats:failed:{0}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm"))));

                    var hourlyFailedKey = String.Format(
                        "hangfire:stats:failed:{0}",
                        DateTime.UtcNow.ToString("yyyy-MM-dd-HH"));
                    transaction.QueueCommand(x => x.IncrementValue(hourlyFailedKey));
                    transaction.QueueCommand(x => x.ExpireEntryIn(hourlyFailedKey, TimeSpan.FromDays(1)));
                }

                transaction.Commit();
            }
        }
    }
}