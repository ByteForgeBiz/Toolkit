namespace ByteForge.Toolkit;
/*
 *  ___ ___ _____   _____                   _ 
 * |_ _/ __/ __\ \ / / _ \___ __ ___ _ _ __| |
 *  | | (__\__ \\ V /|   / -_) _/ _ \ '_/ _` |
 * |___\___|___/ \_/ |_|_\___\__\___/_| \__,_|
 *                                            
 */
/// <summary>
/// Represents a record in a CSV file with validation capabilities.
/// </summary>
/// <remarks>
/// This interface defines the core contract for CSV record objects in the ByteForge CSV processing
/// framework. Classes implementing this interface represent a single row of data from a CSV file,
/// with properties typically mapped to columns using the <see cref="CSVColumnAttribute"/>.
/// <para>
/// The interface focuses on validation capabilities, allowing implementing classes to define
/// custom validation rules and track validation errors. This is particularly useful when
/// processing CSV files that need to meet specific business or data format rules.
/// </para>
/// <para>
/// Classes implementing this interface are typically used with <see cref="CSVReader"/>. 
/// The CSV reader will create instances of the record class and populate them with data from the CSV file.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class PersonRecord : ICSVRecord
/// {
///     public ValidationErrors ValidationErrors { get; } = new ValidationErrors();
/// 
///     [CSVColumn(0, "Name")]
///     public string Name { get; set; }
/// 
///     [CSVColumn(1, "Age")]
///     public int Age { get; set; }
/// 
///     [CSVColumn(2, "Email")]
///     public string Email { get; set; }
/// 
///     public bool IsValid()
///     {
///         ValidationErrors.Clear();
/// 
///         if (string.IsNullOrEmpty(Name))
///             ValidationErrors.Add("Name is required");
/// 
///         if (Age &lt; 0 || Age > 120)
///             ValidationErrors.Add("Age must be between 0 and 120");
/// 
///         if (!string.IsNullOrEmpty(Email) &amp;&amp; !Email.Contains("@"))
///             ValidationErrors.Add("Email format is invalid");
/// 
///         return ValidationErrors.Count == 0;
///     }
/// }
/// 
/// // Using with CSVReader
/// using (var reader = new CSVReader&lt;PersonRecord&gt;("people.csv"))
/// {
///     foreach (var record in reader)
///     {
///         if (record.IsValid())
///         {
///             // Process valid record
///         }
///         else
///         {
///             // Handle validation errors
///             foreach (var error in record.ValidationErrors)
///             {
///                 Console.WriteLine(error);
///             }
///         }
///     }
/// }
/// </code>
/// </example>
public interface ICSVRecord
{
    /// <summary>
    /// Gets the validation errors associated with the record.
    /// </summary>
    /// <remarks>
    /// This property provides access to a collection of validation error messages
    /// that describe why the record is invalid. The collection should be populated
    /// during the <see cref="IsValid"/> method call and can be used to provide
    /// detailed feedback about validation failures.
    /// <para>
    /// Implementations should typically initialize this property with a new instance
    /// of <see cref="ValidationErrors"/> in their constructors to ensure it's never null.
    /// </para>
    /// </remarks>
    ValidationErrors ValidationErrors { get; }

    /// <summary>
    /// Determines whether the record is valid according to business rules.
    /// </summary>
    /// <returns><see langword="true" /> if the record is valid; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// This method evaluates the record against validation rules defined in the 
    /// <see cref="Validate"/> method. If validation fails, the method should add appropriate
    /// error messages to the <see cref="ValidationErrors"/> collection before
    /// returning <see langword="false"/>.
    /// <para>
    /// Implementations should typically clear any existing validation errors at the
    /// beginning of this method to ensure a clean validation state for each call.
    /// </para>
    /// <para>
    /// Common validation checks include:
    /// <list type="bullet">
    ///   <item>Checking for required fields (non-null, non-empty)</item>
    ///   <item>Validating data formats (emails, phone numbers, dates, etc.)</item>
    ///   <item>Range validation for numeric fields</item>
    ///   <item>Cross-field validation (dependencies between fields)</item>
    /// </list>
    /// </para>
    /// </remarks>
    bool IsValid();

    /// <summary>
    /// Validates the current state of the object to ensure it meets the required conditions.
    /// </summary>
    /// <remarks>
    /// This method checks the object's state and throws an exception if any validation rules are violated.<br/>
    /// Call this method before performing operations that depend on the object's validity.
    /// </remarks>
    void Validate();
}
