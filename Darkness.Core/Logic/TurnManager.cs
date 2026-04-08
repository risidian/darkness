using System.Collections.Generic;
using Darkness.Core.Models;

namespace Darkness.Core.Logic;

public class TurnManager
{
    public List<object> TurnOrder { get; set; } = new();
    public int CurrentTurnIndex { get; set; } = 0;
    public int CurrentRound { get; set; } = 1;
    
    public object? CurrentEntity => TurnOrder.Count > 0 && CurrentTurnIndex < TurnOrder.Count ? TurnOrder[CurrentTurnIndex] : null;

    public void Setup(List<Character> party, List<Enemy> enemies, CombatEngine engine)
    {
        TurnOrder = engine.CalculateTurnOrder(party, enemies);
        CurrentTurnIndex = 0;
        CurrentRound = 1;
    }

    public void NextTurn()
    {
        if (TurnOrder.Count == 0) return;
        
        CurrentTurnIndex++;
        if (CurrentTurnIndex >= TurnOrder.Count)
        {
            CurrentTurnIndex = 0;
            CurrentRound++;
        }
    }

    public void RemoveEntity(object entity)
    {
        int index = TurnOrder.IndexOf(entity);
        if (index >= 0)
        {
            TurnOrder.RemoveAt(index);
            // If the entity removed was before the current turn index, decrement it so we don't skip the next person
            if (index < CurrentTurnIndex)
            {
                CurrentTurnIndex--;
            }
            else if (CurrentTurnIndex >= TurnOrder.Count)
            {
                CurrentTurnIndex = 0;
                CurrentRound++;
            }
        }
    }
}
