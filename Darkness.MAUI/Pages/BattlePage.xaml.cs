using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Darkness.Core.Models;
using Darkness.Core.Interfaces;
using Darkness.Core.Logic;

namespace Darkness.MAUI.Pages
{
    public partial class BattlePage : ContentPage
    {
        private readonly ICharacterService _characterService;
        private readonly StoryController _storyController;
        private Character _currentPlayer;
        private List<Character> _party;

        public BattlePage(ICharacterService characterService)
        {
            InitializeComponent();
            _characterService = characterService;
            _storyController = new StoryController();
            
            // Mocking current player for now
            _currentPlayer = new Character
            {
                Name = "The Wanderer",
                Class = "Survivor",
                STR = 12,
                DEX = 10,
                CON = 12,
                INT = 8,
                WIS = 10,
                CHA = 8,
                MaxHP = 100,
                CurrentHP = 100,
                Defense = 5,
                Speed = 10
            };

            _party = new List<Character> { _currentPlayer };

            // For demonstration, let's set a beat. In a real app, this would come from game state.
            _storyController.SetBeat(4); 

            SetupBattle();
        }

        private void SetupBattle()
        {
            var encounter = _storyController.GetEncounterForBeat(_storyController.CurrentBeat);
            var enemies = encounter.Enemies;
            
            foreach (var member in encounter.AdditionalPartyMembers)
            {
                if (!_party.Any(p => p.Name == member.Name))
                {
                    _party.Add(member);
                }
            }

            // In a real implementation, we would pass these to the MonoGame View
            // BattleScene battleScene = new BattleScene(game, _party, enemies, encounter.SurvivalTurns);

            StatusLabel.Text = $"Story Beat {_storyController.CurrentBeat}: Encountered {enemies[0].Name}!";
            if (encounter.SurvivalTurns.HasValue)
            {
                StatusLabel.Text += $" Survive for {encounter.SurvivalTurns} turns!";
            }

            if (_party.Count > 1)
            {
                StatusLabel.Text += $"\nJoined by: {string.Join(", ", _party.Skip(1).Select(p => p.Name))}";
            }
        }

        private async void OnFleeClicked(object sender, EventArgs e)
        {
            bool result = await DisplayAlert("Flee", "Are you sure you want to attempt to flee?", "Yes", "No");
            if (result)
            {
                await Shell.Current.GoToAsync("///GamePage");
            }
        }

        private async void OnContinueClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///GamePage");
        }

        // This would be called by the MonoGame bridge when the battle ends
        public void OnBattleEnded(bool victory)
        {
            MainThread.BeginInvokeOnMainThread(async () => {
                if (victory)
                {
                    StatusLabel.Text = "Victory! The hounds have been silenced.";
                    StatusLabel.TextColor = Colors.Gold;
                }
                else
                {
                    StatusLabel.Text = "Defeat... Darkness consumes you.";
                    StatusLabel.TextColor = Colors.DarkRed;
                }
                
                ContinueButton.IsVisible = true;
            });
        }
    }
}
