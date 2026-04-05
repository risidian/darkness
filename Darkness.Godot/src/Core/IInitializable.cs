using System.Collections.Generic;

namespace Darkness.Godot.Core;

public interface IInitializable
{
    void Initialize(IDictionary<string, object> parameters);
}