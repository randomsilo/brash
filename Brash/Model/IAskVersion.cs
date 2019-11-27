using System;

namespace Brash.Model
{
    public interface IAskVersion
    {
        string GetIdPropertyName();
        string GetGuidPropertyName();
        string GetVersionPropertyName();
    }
}
