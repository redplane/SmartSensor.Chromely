// Copyright © 2017-2020 Chromely Projects. All rights reserved.
// Use of this source code is governed by MIT license that can be found in the LICENSE file.
using System.Threading.Tasks;

namespace Chromely.Core.Network
{
    public interface IChromelyRequestTaskRunner
    {
        #region Methods

        IChromelyResponse Run(IChromelyRequest request);

        Task<IChromelyResponse> RunAsync(IChromelyRequest request);

        #endregion
    }
}
