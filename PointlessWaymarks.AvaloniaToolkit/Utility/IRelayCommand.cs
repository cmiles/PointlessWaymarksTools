using System.Windows.Input;

namespace PointlessWaymarks.AvaloniaToolkit.Utility;

/// <summary>
///     An interface expanding <see cref="ICommand" /> with the ability to raise
///     the <see cref="ICommand.CanExecuteChanged" /> event externally.
///     Not the original source of the 'RelayCommand' but this code has been
///     modified from: the CommunityToolkit.Mvvm.Input -
///     https://github.com/CommunityToolkit/dotnet - MIT License
/// </summary>
public interface IRelayCommand : ICommand
{
    /// <summary>
    ///     Notifies that the <see cref="ICommand.CanExecute" /> property has changed.
    /// </summary>
    void NotifyCanExecuteChanged();
}

/// <summary>
///     A generic interface representing a more specific version of <see cref="IRelayCommand" />.
///     Not the original source of the 'RelayCommand' but this code has been
///     modified from: the CommunityToolkit.Mvvm.Input -
///     https://github.com/CommunityToolkit/dotnet - MIT License
/// </summary>
/// <typeparam name="T">The type used as argument for the interface methods.</typeparam>
public interface IRelayCommand<in T> : IRelayCommand
{
    /// <summary>
    ///     Provides a strongly-typed variant of <see cref="ICommand.CanExecute(object)" />.
    /// </summary>
    /// <param name="parameter">The input parameter.</param>
    /// <returns>Whether the current command can be executed.</returns>
    /// <remarks>Use this overload to avoid boxing, if <typeparamref name="T" /> is a value type.</remarks>
    bool CanExecute(T? parameter);

    /// <summary>
    ///     Provides a strongly-typed variant of <see cref="ICommand.Execute(object)" />.
    /// </summary>
    /// <param name="parameter">The input parameter.</param>
    /// <remarks>Use this overload to avoid boxing, if <typeparamref name="T" /> is a value type.</remarks>
    void Execute(T? parameter);
}