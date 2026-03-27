# Navigation Flow Fix Design

## Problem

After creating an account or logging in, users land on the MainPage (main menu) instead of the CharacterGenPage. The intended flow is: new users should always reach character creation before the main menu.

Root causes:
1. `LoadUserViewModel.LoginAsync` always navigates to `///MainPage`, relying on `MainViewModel.CheckForCharacterAsync` to bounce users to character creation — but the bounce fails because navigation is wrapped in a fire-and-forget `InvokeOnMainThread` async void lambda.
2. `CreateUserViewModel.CreateAccountAsync` navigates back to `LoadUserPage` after account creation, forcing users to log in manually before reaching character creation.

## Design

### 1. Login flow — `LoadUserViewModel`

Inject `ICharacterService`. After successful authentication, check `GetCharactersByUserIdAsync(user.Id)`:
- If no characters exist: navigate to `///CharacterGenPage`
- If characters exist: navigate to `///MainPage`

### 2. Account creation flow — `CreateUserViewModel`

Inject `ISessionService`. After successful account creation:
- Set `_sessionService.CurrentUser = newUser` (auto-login)
- Navigate directly to `///CharacterGenPage` (new accounts never have characters)

### 3. MainPage safety net — `MainViewModel`

Fix `CheckForCharacterAsync`:
- Remove `_dispatcherService.InvokeOnMainThread` wrapper — `OnAppearing` already runs on the main thread, so `await _navigationService.NavigateToAsync(...)` directly.
- In `OnAppearingAsync`, only call `CheckDailyRewardAsync` if the character check passes (user has characters). This prevents the daily reward dialog from racing with a navigation redirect.

### Files changed

| File | Change |
|------|--------|
| `Darkness.Core/ViewModels/LoadUserViewModel.cs` | Add `ICharacterService`; route based on character existence |
| `Darkness.Core/ViewModels/CreateUserViewModel.cs` | Add `ISessionService`; auto-login; navigate to CharacterGenPage |
| `Darkness.Core/ViewModels/MainViewModel.cs` | Remove dispatcher wrapper; gate daily reward on character existence |
