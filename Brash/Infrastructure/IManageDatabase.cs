using System;
using Brash.Model;

namespace Brash.Infrastructure
{
    public interface IManageDatabase
    {
        IDatabaseContext DatabaseContext { get; }
        void CreateDatabase();
    }
}