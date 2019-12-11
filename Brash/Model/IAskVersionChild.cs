using System;

namespace Brash.Model
{
    public interface IAskVersionChild
    {
        string GetParentGuidPropertyName();
        string GetParentVersionPropertyName();
    }
}
