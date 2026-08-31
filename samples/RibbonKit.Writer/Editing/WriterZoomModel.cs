using System.ComponentModel;

namespace RibbonKit.Writer.Editing;

/// <summary>Bounded, UI-independent zoom state for Writer's edit or preview surface.</summary>
/// <remarks>
/// The default range is 25% through 400%, with a 100% default and 10 percentage-point step. Values
/// are clamped to the configured finite range. NaN and infinities are rejected without changing the
/// value. <see cref="PropertyChanged"/> is raised only when <see cref="Value"/> actually changes;
/// this model does not apply a transform to any WPF element.
/// </remarks>
public sealed class WriterZoomModel : INotifyPropertyChanged
{
    private double _value;

    /// <summary>Creates the standard 25%-400% Writer zoom model.</summary>
    public WriterZoomModel() : this(25d, 100d, 400d, 10d)
    {
    }

    /// <summary>Creates a bounded zoom model.</summary>
    /// <param name="minimum">The inclusive minimum percentage, greater than zero.</param>
    /// <param name="defaultValue">The initial percentage within the range.</param>
    /// <param name="maximum">The inclusive maximum percentage.</param>
    /// <param name="step">The positive increment used by <see cref="Increase"/> and <see cref="Decrease"/>.</param>
    public WriterZoomModel(double minimum, double defaultValue, double maximum, double step)
    {
        ValidateFinite(minimum, nameof(minimum));
        ValidateFinite(defaultValue, nameof(defaultValue));
        ValidateFinite(maximum, nameof(maximum));
        ValidateFinite(step, nameof(step));
        if (minimum <= 0)
            throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "The minimum zoom must be positive.");
        if (maximum < minimum)
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum,
                "The maximum zoom cannot be below the minimum zoom.");
        if (step <= 0)
            throw new ArgumentOutOfRangeException(nameof(step), step, "The zoom step must be positive.");
        if (defaultValue < minimum || defaultValue > maximum)
            throw new ArgumentOutOfRangeException(nameof(defaultValue), defaultValue,
                "The default zoom must be inside the configured range.");

        Minimum = minimum;
        Default = defaultValue;
        Maximum = maximum;
        Step = step;
        _value = defaultValue;
    }

    /// <summary>Gets the inclusive minimum percentage.</summary>
    public double Minimum { get; }

    /// <summary>Gets the configured default percentage.</summary>
    public double Default { get; }

    /// <summary>Gets the inclusive maximum percentage.</summary>
    public double Maximum { get; }

    /// <summary>Gets the percentage-point increment used by relative operations.</summary>
    public double Step { get; }

    /// <summary>Gets or sets the current clamped zoom percentage.</summary>
    /// <remarks>Invalid non-finite assignments are ignored.</remarks>
    public double Value
    {
        get => _value;
        set => TrySet(value);
    }

    /// <summary>Raised only when <see cref="Value"/> changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Attempts to set a finite zoom percentage, clamping it to the configured bounds.</summary>
    /// <returns><see langword="true"/> when the value changed; otherwise <see langword="false"/>.</returns>
    public bool TrySet(double value)
    {
        if (!double.IsFinite(value))
            return false;
        var clamped = Math.Clamp(value, Minimum, Maximum);
        if (clamped == _value)
            return false;
        _value = clamped;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        return true;
    }

    /// <summary>Increases zoom by one step and clamps at <see cref="Maximum"/>.</summary>
    /// <returns><see langword="true"/> when the value changed.</returns>
    public bool Increase() => TrySet(Step > Maximum - _value ? Maximum : _value + Step);

    /// <summary>Decreases zoom by one step and clamps at <see cref="Minimum"/>.</summary>
    /// <returns><see langword="true"/> when the value changed.</returns>
    public bool Decrease() => TrySet(Step > _value - Minimum ? Minimum : _value - Step);

    /// <summary>Restores the configured default percentage.</summary>
    /// <returns><see langword="true"/> when the value changed.</returns>
    public bool Reset() => TrySet(Default);

    private static void ValidateFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(parameterName, value, "Zoom values must be finite.");
    }
}
