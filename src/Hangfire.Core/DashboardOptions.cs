﻿// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using Hangfire.Dashboard;

namespace Hangfire
{
    public class DashboardOptions
    {
        public DashboardOptions()
        {
            AppPath = "/";
            EnableSearch = false;
            RelativeTime = true;
            SpecificJobRequeue = false;
            AuthorizationFilters = new[] { new LocalRequestsOnlyAuthorizationFilter() };
        }

        /// <summary>
        /// The path for the Back To Site link. Set to <see langword="null" /> in order to hide the Back To Site link.
        /// Options for disabling/enabling new functionality on the dashboard.
        /// </summary>
        public string AppPath { get; set; }
        public bool EnableSearch { get; set; }
        public bool RelativeTime { get; set; }
        public bool SpecificJobRequeue { get; set; }

        public IEnumerable<IAuthorizationFilter> AuthorizationFilters { get; set; }
    }
}