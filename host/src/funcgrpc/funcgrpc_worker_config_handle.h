// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <string>

namespace funcgrpc
{
class WorkerConfigHandle
{
  public:
    WorkerConfigHandle() = default;

    ~WorkerConfigHandle();

    /**
     * Gets the full path to the function app executable.
     * @param dir Path to function app directory.
     */
    std::string GetApplicationExePath(const std::string &dir);

  private:
    FILE *fp;
    void closeFileHandle();
};
} // namespace funcgrpc
