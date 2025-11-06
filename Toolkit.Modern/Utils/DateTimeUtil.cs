using System;
using System.Collections.Generic;

namespace ByteForge.Toolkit;
/*
 *  ___       _      _____ _           _   _ _   _ _ 
 * |   \ __ _| |_ __|_   _(_)_ __  ___| | | | |_(_) |
 * | |) / _` |  _/ -_)| | | | '  \/ -_) |_| |  _| | |
 * |___/\__,_|\__\___||_| |_|_|_|_\___|\___/ \__|_|_|
 *                                                   
 */
/// <summary>
/// Provides utility methods for working with <see cref="DateTime"/> and time zone conversions.
/// </summary>
public static class DateTimeUtil
{
    /// <summary>
    /// The Unix epoch (January 1, 1970, UTC).
    /// </summary>
    private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Converts a Unix timestamp (seconds since Unix epoch) to a <see cref="DateTime"/> in UTC.
    /// </summary>
    /// <param name="unixTime">The Unix timestamp in seconds.</param>
    /// <returns>
    /// A <see cref="DateTime"/> representing the specified Unix time in UTC.
    /// </returns>
    public static DateTime FromUnixTime(double unixTime)
    {
        return UnixEpoch.AddSeconds(unixTime);
    }

    /// <summary>
    /// Converts a Unix timestamp in milliseconds since the Unix epoch to a <see cref="DateTime"/> in UTC.
    /// </summary>
    /// <param name="unixTimeMilliseconds">The Unix timestamp in milliseconds.</param>
    /// <returns>
    /// A <see cref="DateTime"/> representing the specified Unix time in UTC.
    /// </returns>
    public static DateTime FromUnixTimeMilliseconds(double unixTimeMilliseconds)
    {
        return UnixEpoch.AddMilliseconds(unixTimeMilliseconds);
    }

    /// <summary>
    /// Converts a <see cref="DateTime"/> to a Unix timestamp (seconds since Unix epoch).
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> to convert. If not UTC, it will be treated as UTC.</param>
    /// <returns>The Unix timestamp in seconds.</returns>
    public static double ToUnixTime(this DateTime dateTime)
    {
        dateTime = AssureUtc(dateTime);
        return (dateTime - UnixEpoch).TotalSeconds;
    }

    /// <summary>
    /// Converts a <see cref="DateTime"/> to a Unix timestamp in milliseconds since the Unix epoch.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> to convert. If not UTC, it will be treated as UTC.</param>
    /// <returns>The Unix timestamp in milliseconds.</returns>
    public static double ToUnixTimeMilliseconds(this DateTime dateTime)
    {
        dateTime = AssureUtc(dateTime);
        return (dateTime - UnixEpoch).TotalMilliseconds;
    }

    /// <summary>
    /// Assures the provided <see cref="DateTime"/> is in UTC. If not, converts it to UTC.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> to check and convert if necessary.</param>
    /// <returns>The <see cref="DateTime"/> in UTC.</returns>
    private static DateTime AssureUtc(DateTime dateTime) => dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime();

    /// <summary>
    /// Determines whether the specified <see cref="DateTime"/> instance includes a time component other than midnight.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> instance to evaluate.</param>
    /// <returns>
    /// <see langword="true"/> if the <paramref name="dateTime"/> has a time component other than midnight; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool HasTimeComponent(this DateTime dateTime)
    {
        // Check if the time component is not midnight (00:00:00)
        return dateTime.TimeOfDay != TimeSpan.Zero;
    }

    /// <summary>
    /// Maps time zone abbreviations and offsets to Windows TimeZone IDs.
    /// </summary>
    private static readonly Dictionary<string, string> TimeZoneAbbreviations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // UTC Offsets
        { "+00", "UTC" },
        { "+01", "W. Europe Standard Time" },
        { "+02", "South Africa Standard Time" },
        { "+03", "Russian Standard Time" },
        { "+0330", "Iran Standard Time" },
        { "+04", "Arabian Standard Time" },
        { "+0430", "Afghanistan Standard Time" },
        { "+05", "West Asia Standard Time" },
        { "+0530", "India Standard Time" },
        { "+0545", "Nepal Standard Time" },
        { "+06", "Central Asia Standard Time" },
        { "+0630", "Myanmar Standard Time" },
        { "+07", "SE Asia Standard Time" },
        { "+08", "China Standard Time" },
        { "+0845", "Aus Central W. Standard Time" },
        { "+09", "Tokyo Standard Time" },
        { "+10", "AUS Eastern Standard Time" },
        { "+1030", "Lord Howe Standard Time" },
        { "+11", "Central Pacific Standard Time" },
        { "+12", "Fiji Standard Time" },
        { "+1245", "Chatham Islands Standard Time" },
        { "+13", "Tonga Standard Time" },
        { "+1345", "Chatham Islands Standard Time" },
        { "+14", "Line Islands Standard Time" },
        { "-01", "Azores Standard Time" },
        { "-02", "UTC-02" },
        { "-03", "E. South America Standard Time" },
        { "-04", "Atlantic Standard Time" },
        { "-05", "Eastern Standard Time" },
        { "-06", "Central Standard Time" },
        { "-07", "Mountain Standard Time" },
        { "-08", "Pacific Standard Time" },
        { "-09", "Alaskan Standard Time" },
        { "-0930", "Marquesas Standard Time" },
        { "-10", "Hawaiian Standard Time" },
        { "-11", "UTC-11" },
        { "-12", "Dateline Standard Time" },

        // Standard Abbreviations
        { "ACDT", "Cen. Australia Standard Time" },
        { "ACST", "AUS Central Standard Time" },
        { "ADT", "Atlantic Standard Time" },
        { "AEDT", "AUS Eastern Standard Time" },
        { "AEST", "AUS Eastern Standard Time" },
        { "AKDT", "Alaskan Standard Time" },
        { "AKST", "Alaskan Standard Time" },
        { "AST", "Atlantic Standard Time" },
        { "AWST", "W. Australia Standard Time" },
        { "BST", "GMT Standard Time" },
        { "CAT", "South Africa Standard Time" },
        { "CDT", "Central Standard Time" },
        { "CEST", "W. Europe Standard Time" },
        { "CET", "W. Europe Standard Time" },
        { "ChST", "West Pacific Standard Time" },
        { "CST", "Central Standard Time" },
        { "EAT", "E. Africa Standard Time" },
        { "EDT", "Eastern Standard Time" },
        { "EEST", "GTB Standard Time" },
        { "EET", "GTB Standard Time" },
        { "EST", "Eastern Standard Time" },
        { "GMT", "GMT Standard Time" },
        { "HDT", "Aleutian Standard Time" },
        { "HKT", "China Standard Time" },
        { "HST", "Hawaiian Standard Time" },
        { "IDT", "Israel Standard Time" },
        { "IST", "India Standard Time" },
        { "JST", "Tokyo Standard Time" },
        { "KST", "Korea Standard Time" },
        { "MDT", "Mountain Standard Time" },
        { "MSK", "Russian Standard Time" },
        { "MST", "Mountain Standard Time" },
        { "NDT", "Newfoundland Standard Time" },
        { "NST", "Newfoundland Standard Time" },
        { "NZDT", "New Zealand Standard Time" },
        { "NZST", "New Zealand Standard Time" },
        { "PDT", "Pacific Standard Time" },
        { "PKT", "Pakistan Standard Time" },
        { "PST", "Pacific Standard Time" },
        { "SAST", "South Africa Standard Time" },
        { "SST", "UTC-11" },
        { "UTC", "UTC" },
        { "WAT", "W. Central Africa Standard Time" },
        { "WEST", "GMT Standard Time" },
        { "WET", "GMT Standard Time" },
        { "WIB", "SE Asia Standard Time" },
        { "WIT", "Tokyo Standard Time" },
        { "WITA", "Singapore Standard Time" }
    };

    /// <summary>
    /// Maps IANA timezone codes to Windows TimeZone IDs.
    /// </summary>
    private static readonly Dictionary<string, string> IanaToWindowsTimeZone = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // UTC-12:00 International Date Line West
        { "Etc/GMT+12", "Dateline Standard Time" },

        // UTC-11:00 Coordinated Universal Time-11
        { "Etc/GMT+11", "UTC-11" },
        { "Pacific/Pago_Pago", "UTC-11" },
        { "Pacific/Niue", "UTC-11" },
        { "Pacific/Midway", "UTC-11" },

        // UTC-10:00 Aleutian Islands
        { "America/Adak", "Aleutian Standard Time" },

        // UTC-10:00 Hawaii
        { "Pacific/Honolulu", "Hawaiian Standard Time" },
        { "Pacific/Rarotonga", "Hawaiian Standard Time" },
        { "Pacific/Tahiti", "Hawaiian Standard Time" },
        { "Etc/GMT+10", "Hawaiian Standard Time" },

        // UTC-09:30 Marquesas Islands
        { "Pacific/Marquesas", "Marquesas Standard Time" },

        // UTC-09:00 Alaska
        { "America/Anchorage", "Alaskan Standard Time" },
        { "America/Juneau", "Alaskan Standard Time" },
        { "America/Metlakatla", "Alaskan Standard Time" },
        { "America/Nome", "Alaskan Standard Time" },
        { "America/Sitka", "Alaskan Standard Time" },
        { "America/Yakutat", "Alaskan Standard Time" },

        // UTC-09:00 Coordinated Universal Time-09
        { "Etc/GMT+9", "UTC-09" },
        { "Pacific/Gambier", "UTC-09" },

        // UTC-08:00 Baja California
        { "America/Tijuana", "Pacific Standard Time (Mexico)" },

        // UTC-08:00 Coordinated Universal Time-08
        { "Etc/GMT+8", "UTC-08" },
        { "Pacific/Pitcairn", "UTC-08" },

        // UTC-08:00 Pacific Time (US & Canada)
        { "America/Los_Angeles", "Pacific Standard Time" },
        { "America/Vancouver", "Pacific Standard Time" },

        // UTC-07:00 Arizona
        { "America/Phoenix", "US Mountain Standard Time" },
        { "America/Creston", "US Mountain Standard Time" },
        { "America/Dawson_Creek", "US Mountain Standard Time" },
        { "America/Fort_Nelson", "US Mountain Standard Time" },
        { "America/Hermosillo", "US Mountain Standard Time" },
        { "Etc/GMT+7", "US Mountain Standard Time" },

        // UTC-07:00 Chihuahua, La Paz, Mazatlan
        { "America/Mazatlan", "Mountain Standard Time (Mexico)" },

        // UTC-07:00 Mountain Time (US & Canada)
        { "America/Denver", "Mountain Standard Time" },
        { "America/Edmonton", "Mountain Standard Time" },
        { "America/Cambridge_Bay", "Mountain Standard Time" },
        { "America/Inuvik", "Mountain Standard Time" },
        { "America/Ciudad_Juarez", "Mountain Standard Time" },
        { "America/Boise", "Mountain Standard Time" },

        // UTC-07:00 Yukon
        { "America/Whitehorse", "Yukon Standard Time" },
        { "America/Dawson", "Yukon Standard Time" },

        // UTC-06:00 Central America
        { "America/Guatemala", "Central America Standard Time" },
        { "America/Belize", "Central America Standard Time" },
        { "America/Costa_Rica", "Central America Standard Time" },
        { "Pacific/Galapagos", "Central America Standard Time" },
        { "America/Tegucigalpa", "Central America Standard Time" },
        { "America/Managua", "Central America Standard Time" },
        { "America/El_Salvador", "Central America Standard Time" },
        { "Etc/GMT+6", "Central America Standard Time" },

        // UTC-06:00 Central Time (US & Canada)
        { "America/Chicago", "Central Standard Time" },
        { "America/Winnipeg", "Central Standard Time" },
        { "America/Rankin_Inlet", "Central Standard Time" },
        { "America/Resolute", "Central Standard Time" },
        { "America/Matamoros", "Central Standard Time" },
        { "America/Ojinaga", "Central Standard Time" },
        { "America/Indiana/Knox", "Central Standard Time" },
        { "America/Indiana/Tell_City", "Central Standard Time" },
        { "America/Menominee", "Central Standard Time" },
        { "America/North_Dakota/Beulah", "Central Standard Time" },
        { "America/North_Dakota/Center", "Central Standard Time" },
        { "America/North_Dakota/New_Salem", "Central Standard Time" },

        // UTC-06:00 Easter Island
        { "Pacific/Easter", "Easter Island Standard Time" },

        // UTC-06:00 Guadalajara, Mexico City, Monterrey
        { "America/Mexico_City", "Central Standard Time (Mexico)" },
        { "America/Bahia_Banderas", "Central Standard Time (Mexico)" },
        { "America/Merida", "Central Standard Time (Mexico)" },
        { "America/Monterrey", "Central Standard Time (Mexico)" },
        { "America/Chihuahua", "Central Standard Time (Mexico)" },

        // UTC-06:00 Saskatchewan
        { "America/Regina", "Canada Central Standard Time" },
        { "America/Swift_Current", "Canada Central Standard Time" },

        // UTC-05:00 Bogota, Lima, Quito, Rio Branco
        { "America/Bogota", "SA Pacific Standard Time" },
        { "America/Rio_Branco", "SA Pacific Standard Time" },
        { "America/Eirunepe", "SA Pacific Standard Time" },
        { "America/Coral_Harbour", "SA Pacific Standard Time" },
        { "America/Guayaquil", "SA Pacific Standard Time" },
        { "America/Jamaica", "SA Pacific Standard Time" },
        { "America/Cayman", "SA Pacific Standard Time" },
        { "America/Panama", "SA Pacific Standard Time" },
        { "America/Lima", "SA Pacific Standard Time" },
        { "Etc/GMT+5", "SA Pacific Standard Time" },

        // UTC-05:00 Chetumal
        { "America/Cancun", "Eastern Standard Time (Mexico)" },

        // UTC-05:00 Eastern Time (US & Canada)
        { "America/New_York", "Eastern Standard Time" },
        { "America/Nassau", "Eastern Standard Time" },
        { "America/Toronto", "Eastern Standard Time" },
        { "America/Iqaluit", "Eastern Standard Time" },
        { "America/Detroit", "Eastern Standard Time" },
        { "America/Indiana/Petersburg", "Eastern Standard Time" },
        { "America/Indiana/Vincennes", "Eastern Standard Time" },
        { "America/Indiana/Winamac", "Eastern Standard Time" },
        { "America/Kentucky/Monticello", "Eastern Standard Time" },
        { "America/Louisville", "Eastern Standard Time" },

        // UTC-05:00 Haiti
        { "America/Port-au-Prince", "Haiti Standard Time" },

        // UTC-05:00 Havana
        { "America/Havana", "Cuba Standard Time" },

        // UTC-05:00 Indiana (East)
        { "America/Indianapolis", "US Eastern Standard Time" },
        { "America/Indiana/Marengo", "US Eastern Standard Time" },
        { "America/Indiana/Vevay", "US Eastern Standard Time" },

        // UTC-05:00 Turks and Caicos
        { "America/Grand_Turk", "Turks And Caicos Standard Time" },

        // UTC-04:00 Asuncion
        { "America/Asuncion", "Paraguay Standard Time" },

        // UTC-04:00 Atlantic Time (Canada)
        { "America/Halifax", "Atlantic Standard Time" },
        { "Atlantic/Bermuda", "Atlantic Standard Time" },
        { "America/Glace_Bay", "Atlantic Standard Time" },
        { "America/Goose_Bay", "Atlantic Standard Time" },
        { "America/Moncton", "Atlantic Standard Time" },
        { "America/Thule", "Atlantic Standard Time" },

        // UTC-04:00 Caracas
        { "America/Caracas", "Venezuela Standard Time" },

        // UTC-04:00 Cuiaba
        { "America/Cuiaba", "Central Brazilian Standard Time" },
        { "America/Campo_Grande", "Central Brazilian Standard Time" },

        // UTC-04:00 Georgetown, La Paz, Manaus, San Juan
        { "America/La_Paz", "SA Western Standard Time" },
        { "America/Antigua", "SA Western Standard Time" },
        { "America/Anguilla", "SA Western Standard Time" },
        { "America/Aruba", "SA Western Standard Time" },
        { "America/Barbados", "SA Western Standard Time" },
        { "America/St_Barthelemy", "SA Western Standard Time" },
        { "America/Kralendijk", "SA Western Standard Time" },
        { "America/Manaus", "SA Western Standard Time" },
        { "America/Boa_Vista", "SA Western Standard Time" },
        { "America/Porto_Velho", "SA Western Standard Time" },
        { "America/Blanc-Sablon", "SA Western Standard Time" },
        { "America/Curacao", "SA Western Standard Time" },
        { "America/Dominica", "SA Western Standard Time" },
        { "America/Santo_Domingo", "SA Western Standard Time" },
        { "America/Grenada", "SA Western Standard Time" },
        { "America/Guadeloupe", "SA Western Standard Time" },
        { "America/Guyana", "SA Western Standard Time" },
        { "America/St_Kitts", "SA Western Standard Time" },
        { "America/St_Lucia", "SA Western Standard Time" },
        { "America/Marigot", "SA Western Standard Time" },
        { "America/Martinique", "SA Western Standard Time" },
        { "America/Montserrat", "SA Western Standard Time" },
        { "America/Puerto_Rico", "SA Western Standard Time" },
        { "America/Lower_Princes", "SA Western Standard Time" },
        { "America/Port_of_Spain", "SA Western Standard Time" },
        { "America/St_Vincent", "SA Western Standard Time" },
        { "America/Tortola", "SA Western Standard Time" },
        { "America/St_Thomas", "SA Western Standard Time" },
        { "Etc/GMT+4", "SA Western Standard Time" },

        // UTC-04:00 Santiago
        { "America/Santiago", "Pacific SA Standard Time" },

        // UTC-03:30 Newfoundland
        { "America/St_Johns", "Newfoundland Standard Time" },

        // UTC-03:00 Araguaina
        { "America/Araguaina", "Tocantins Standard Time" },

        // UTC-03:00 Brasilia
        { "America/Sao_Paulo", "E. South America Standard Time" },

        // UTC-03:00 Cayenne, Fortaleza
        { "America/Cayenne", "SA Eastern Standard Time" },
        { "Antarctica/Rothera", "SA Eastern Standard Time" },
        { "Antarctica/Palmer", "SA Eastern Standard Time" },
        { "America/Fortaleza", "SA Eastern Standard Time" },
        { "America/Belem", "SA Eastern Standard Time" },
        { "America/Maceio", "SA Eastern Standard Time" },
        { "America/Recife", "SA Eastern Standard Time" },
        { "America/Santarem", "SA Eastern Standard Time" },
        { "Atlantic/Stanley", "SA Eastern Standard Time" },
        { "America/Paramaribo", "SA Eastern Standard Time" },
        { "Etc/GMT+3", "SA Eastern Standard Time" },

        // UTC-03:00 City of Buenos Aires
        { "America/Buenos_Aires", "Argentina Standard Time" },
        { "America/Argentina/La_Rioja", "Argentina Standard Time" },
        { "America/Argentina/Rio_Gallegos", "Argentina Standard Time" },
        { "America/Argentina/Salta", "Argentina Standard Time" },
        { "America/Argentina/San_Juan", "Argentina Standard Time" },
        { "America/Argentina/San_Luis", "Argentina Standard Time" },
        { "America/Argentina/Tucuman", "Argentina Standard Time" },
        { "America/Argentina/Ushuaia", "Argentina Standard Time" },
        { "America/Catamarca", "Argentina Standard Time" },
        { "America/Cordoba", "Argentina Standard Time" },
        { "America/Jujuy", "Argentina Standard Time" },
        { "America/Mendoza", "Argentina Standard Time" },

        // UTC-03:00 Greenland
        { "America/Godthab", "Greenland Standard Time" },

        // UTC-03:00 Montevideo
        { "America/Montevideo", "Montevideo Standard Time" },

        // UTC-03:00 Punta Arenas
        { "America/Punta_Arenas", "Magallanes Standard Time" },
        { "America/Coyhaique", "Magallanes Standard Time" },

        // UTC-03:00 Saint Pierre and Miquelon
        { "America/Miquelon", "Saint Pierre Standard Time" },

        // UTC-03:00 Salvador
        { "America/Bahia", "Bahia Standard Time" },

        // UTC-02:00 Coordinated Universal Time-02
        { "Etc/GMT+2", "UTC-02" },
        { "America/Noronha", "UTC-02" },
        { "Atlantic/South_Georgia", "UTC-02" },

        // UTC-01:00 Azores
        { "Atlantic/Azores", "Azores Standard Time" },
        { "America/Scoresbysund", "Azores Standard Time" },

        // UTC-01:00 Cabo Verde Is.
        { "Atlantic/Cape_Verde", "Cape Verde Standard Time" },
        { "Etc/GMT+1", "Cape Verde Standard Time" },

        // UTC Coordinated Universal Time
        { "Etc/UTC", "UTC" },
        { "Etc/GMT", "UTC" },

        // UTC+00:00 Dublin, Edinburgh, Lisbon, London
        { "Europe/London", "GMT Standard Time" },
        { "Atlantic/Canary", "GMT Standard Time" },
        { "Atlantic/Faeroe", "GMT Standard Time" },
        { "Europe/Guernsey", "GMT Standard Time" },
        { "Europe/Dublin", "GMT Standard Time" },
        { "Europe/Isle_of_Man", "GMT Standard Time" },
        { "Europe/Jersey", "GMT Standard Time" },
        { "Europe/Lisbon", "GMT Standard Time" },
        { "Atlantic/Madeira", "GMT Standard Time" },

        // UTC+00:00 Monrovia, Reykjavik
        { "Atlantic/Reykjavik", "Greenwich Standard Time" },
        { "Africa/Ouagadougou", "Greenwich Standard Time" },
        { "Africa/Abidjan", "Greenwich Standard Time" },
        { "Africa/Accra", "Greenwich Standard Time" },
        { "America/Danmarkshavn", "Greenwich Standard Time" },
        { "Africa/Banjul", "Greenwich Standard Time" },
        { "Africa/Conakry", "Greenwich Standard Time" },
        { "Africa/Bissau", "Greenwich Standard Time" },
        { "Africa/Monrovia", "Greenwich Standard Time" },
        { "Africa/Bamako", "Greenwich Standard Time" },
        { "Africa/Nouakchott", "Greenwich Standard Time" },
        { "Atlantic/St_Helena", "Greenwich Standard Time" },
        { "Africa/Freetown", "Greenwich Standard Time" },
        { "Africa/Dakar", "Greenwich Standard Time" },
        { "Africa/Lome", "Greenwich Standard Time" },

        // UTC+00:00 Sao Tome
        { "Africa/Sao_Tome", "Sao Tome Standard Time" },

        // UTC+01:00 Casablanca
        { "Africa/Casablanca", "Morocco Standard Time" },
        { "Africa/El_Aaiun", "Morocco Standard Time" },

        // UTC+01:00 Amsterdam, Berlin, Bern, Rome, Stockholm, Vienna
        { "Europe/Berlin", "W. Europe Standard Time" },
        { "Europe/Andorra", "W. Europe Standard Time" },
        { "Europe/Vienna", "W. Europe Standard Time" },
        { "Europe/Zurich", "W. Europe Standard Time" },
        { "Europe/Busingen", "W. Europe Standard Time" },
        { "Europe/Gibraltar", "W. Europe Standard Time" },
        { "Europe/Rome", "W. Europe Standard Time" },
        { "Europe/Vaduz", "W. Europe Standard Time" },
        { "Europe/Luxembourg", "W. Europe Standard Time" },
        { "Europe/Monaco", "W. Europe Standard Time" },
        { "Europe/Malta", "W. Europe Standard Time" },
        { "Europe/Amsterdam", "W. Europe Standard Time" },
        { "Europe/Oslo", "W. Europe Standard Time" },
        { "Europe/Stockholm", "W. Europe Standard Time" },
        { "Arctic/Longyearbyen", "W. Europe Standard Time" },
        { "Europe/San_Marino", "W. Europe Standard Time" },
        { "Europe/Vatican", "W. Europe Standard Time" },

        // UTC+01:00 Belgrade, Bratislava, Budapest, Ljubljana, Prague
        { "Europe/Budapest", "Central Europe Standard Time" },
        { "Europe/Tirane", "Central Europe Standard Time" },
        { "Europe/Prague", "Central Europe Standard Time" },
        { "Europe/Podgorica", "Central Europe Standard Time" },
        { "Europe/Belgrade", "Central Europe Standard Time" },
        { "Europe/Ljubljana", "Central Europe Standard Time" },
        { "Europe/Bratislava", "Central Europe Standard Time" },

        // UTC+01:00 Brussels, Copenhagen, Madrid, Paris
        { "Europe/Paris", "Romance Standard Time" },
        { "Europe/Brussels", "Romance Standard Time" },
        { "Europe/Copenhagen", "Romance Standard Time" },
        { "Europe/Madrid", "Romance Standard Time" },
        { "Africa/Ceuta", "Romance Standard Time" },

        // UTC+01:00 Sarajevo, Skopje, Warsaw, Zagreb
        { "Europe/Warsaw", "Central European Standard Time" },
        { "Europe/Sarajevo", "Central European Standard Time" },
        { "Europe/Zagreb", "Central European Standard Time" },
        { "Europe/Skopje", "Central European Standard Time" },

        // UTC+01:00 West Central Africa
        { "Africa/Lagos", "W. Central Africa Standard Time" },
        { "Africa/Luanda", "W. Central Africa Standard Time" },
        { "Africa/Porto-Novo", "W. Central Africa Standard Time" },
        { "Africa/Kinshasa", "W. Central Africa Standard Time" },
        { "Africa/Bangui", "W. Central Africa Standard Time" },
        { "Africa/Brazzaville", "W. Central Africa Standard Time" },
        { "Africa/Douala", "W. Central Africa Standard Time" },
        { "Africa/Algiers", "W. Central Africa Standard Time" },
        { "Africa/Libreville", "W. Central Africa Standard Time" },
        { "Africa/Malabo", "W. Central Africa Standard Time" },
        { "Africa/Niamey", "W. Central Africa Standard Time" },
        { "Africa/Ndjamena", "W. Central Africa Standard Time" },
        { "Africa/Tunis", "W. Central Africa Standard Time" },
        { "Etc/GMT-1", "W. Central Africa Standard Time" },

        // UTC+02:00 Amman
        { "Asia/Amman", "Jordan Standard Time" },

        // UTC+02:00 Athens, Bucharest
        { "Europe/Bucharest", "GTB Standard Time" },
        { "Asia/Nicosia", "GTB Standard Time" },
        { "Asia/Famagusta", "GTB Standard Time" },
        { "Europe/Athens", "GTB Standard Time" },

        // UTC+02:00 Beirut
        { "Asia/Beirut", "Middle East Standard Time" },

        // UTC+02:00 Cairo
        { "Africa/Cairo", "Egypt Standard Time" },

        // UTC+02:00 Chisinau
        { "Europe/Chisinau", "E. Europe Standard Time" },

        // UTC+02:00 Damascus
        { "Asia/Damascus", "Syria Standard Time" },

        // UTC+02:00 Gaza, Hebron
        { "Asia/Hebron", "West Bank Standard Time" },
        { "Asia/Gaza", "West Bank Standard Time" },

        // UTC+02:00 Harare, Pretoria
        { "Africa/Johannesburg", "South Africa Standard Time" },
        { "Africa/Bujumbura", "South Africa Standard Time" },
        { "Africa/Gaborone", "South Africa Standard Time" },
        { "Africa/Lubumbashi", "South Africa Standard Time" },
        { "Africa/Maseru", "South Africa Standard Time" },
        { "Africa/Blantyre", "South Africa Standard Time" },
        { "Africa/Maputo", "South Africa Standard Time" },
        { "Africa/Kigali", "South Africa Standard Time" },
        { "Africa/Mbabane", "South Africa Standard Time" },
        { "Africa/Lusaka", "South Africa Standard Time" },
        { "Africa/Harare", "South Africa Standard Time" },
        { "Etc/GMT-2", "South Africa Standard Time" },

        // UTC+02:00 Helsinki, Kyiv, Riga, Sofia, Tallinn, Vilnius
        { "Europe/Kiev", "FLE Standard Time" },
        { "Europe/Mariehamn", "FLE Standard Time" },
        { "Europe/Sofia", "FLE Standard Time" },
        { "Europe/Tallinn", "FLE Standard Time" },
        { "Europe/Helsinki", "FLE Standard Time" },
        { "Europe/Vilnius", "FLE Standard Time" },
        { "Europe/Riga", "FLE Standard Time" },

        // UTC+02:00 Jerusalem
        { "Asia/Jerusalem", "Israel Standard Time" },

        // UTC+02:00 Juba
        { "Africa/Juba", "South Sudan Standard Time" },

        // UTC+02:00 Kaliningrad
        { "Europe/Kaliningrad", "Kaliningrad Standard Time" },

        // UTC+02:00 Khartoum
        { "Africa/Khartoum", "Sudan Standard Time" },

        // UTC+02:00 Tripoli
        { "Africa/Tripoli", "Libya Standard Time" },

        // UTC+02:00 Windhoek
        { "Africa/Windhoek", "Namibia Standard Time" },

        // UTC+03:00 Baghdad
        { "Asia/Baghdad", "Arabic Standard Time" },

        // UTC+03:00 Istanbul
        { "Europe/Istanbul", "Turkey Standard Time" },

        // UTC+03:00 Kuwait, Riyadh
        { "Asia/Riyadh", "Arab Standard Time" },
        { "Asia/Bahrain", "Arab Standard Time" },
        { "Asia/Kuwait", "Arab Standard Time" },
        { "Asia/Qatar", "Arab Standard Time" },
        { "Asia/Aden", "Arab Standard Time" },

        // UTC+03:00 Minsk
        { "Europe/Minsk", "Belarus Standard Time" },

        // UTC+03:00 Moscow, St. Petersburg
        { "Europe/Moscow", "Russian Standard Time" },
        { "Europe/Kirov", "Russian Standard Time" },
        { "Europe/Simferopol", "Russian Standard Time" },

        // UTC+03:00 Nairobi
        { "Africa/Nairobi", "E. Africa Standard Time" },
        { "Antarctica/Syowa", "E. Africa Standard Time" },
        { "Africa/Djibouti", "E. Africa Standard Time" },
        { "Africa/Asmera", "E. Africa Standard Time" },
        { "Africa/Addis_Ababa", "E. Africa Standard Time" },
        { "Indian/Comoro", "E. Africa Standard Time" },
        { "Indian/Antananarivo", "E. Africa Standard Time" },
        { "Africa/Mogadishu", "E. Africa Standard Time" },
        { "Africa/Dar_es_Salaam", "E. Africa Standard Time" },
        { "Africa/Kampala", "E. Africa Standard Time" },
        { "Indian/Mayotte", "E. Africa Standard Time" },
        { "Etc/GMT-3", "E. Africa Standard Time" },

        // UTC+03:30 Tehran
        { "Asia/Tehran", "Iran Standard Time" },

        // UTC+04:00 Abu Dhabi, Muscat
        { "Asia/Dubai", "Arabian Standard Time" },
        { "Asia/Muscat", "Arabian Standard Time" },
        { "Etc/GMT-4", "Arabian Standard Time" },

        // UTC+04:00 Astrakhan, Ulyanovsk
        { "Europe/Astrakhan", "Astrakhan Standard Time" },
        { "Europe/Ulyanovsk", "Astrakhan Standard Time" },

        // UTC+04:00 Baku
        { "Asia/Baku", "Azerbaijan Standard Time" },

        // UTC+04:00 Izhevsk, Samara
        { "Europe/Samara", "Russia Time Zone 3" },

        // UTC+04:00 Port Louis
        { "Indian/Mauritius", "Mauritius Standard Time" },
        { "Indian/Reunion", "Mauritius Standard Time" },
        { "Indian/Mahe", "Mauritius Standard Time" },

        // UTC+04:00 Saratov
        { "Europe/Saratov", "Saratov Standard Time" },

        // UTC+04:00 Tbilisi
        { "Asia/Tbilisi", "Georgian Standard Time" },

        // UTC+04:00 Volgograd
        { "Europe/Volgograd", "Volgograd Standard Time" },

        // UTC+04:00 Yerevan
        { "Asia/Yerevan", "Caucasus Standard Time" },

        // UTC+04:30 Kabul
        { "Asia/Kabul", "Afghanistan Standard Time" },

        // UTC+05:00 Ashgabat, Tashkent
        { "Asia/Tashkent", "West Asia Standard Time" },
        { "Antarctica/Mawson", "West Asia Standard Time" },
        { "Asia/Oral", "West Asia Standard Time" },
        { "Asia/Almaty", "West Asia Standard Time" },
        { "Asia/Aqtau", "West Asia Standard Time" },
        { "Asia/Aqtobe", "West Asia Standard Time" },
        { "Asia/Atyrau", "West Asia Standard Time" },
        { "Asia/Qostanay", "West Asia Standard Time" },
        { "Indian/Maldives", "West Asia Standard Time" },
        { "Indian/Kerguelen", "West Asia Standard Time" },
        { "Asia/Dushanbe", "West Asia Standard Time" },
        { "Asia/Ashgabat", "West Asia Standard Time" },
        { "Asia/Samarkand", "West Asia Standard Time" },
        { "Etc/GMT-5", "West Asia Standard Time" },

        // UTC+05:00 Ekaterinburg
        { "Asia/Yekaterinburg", "Ekaterinburg Standard Time" },

        // UTC+05:00 Islamabad, Karachi
        { "Asia/Karachi", "Pakistan Standard Time" },

        // UTC+05:00 Qyzylorda
        { "Asia/Qyzylorda", "Qyzylorda Standard Time" },

        // UTC+05:30 Chennai, Kolkata, Mumbai, New Delhi
        { "Asia/Calcutta", "India Standard Time" },

        // UTC+05:30 Sri Jayawardenepura
        { "Asia/Colombo", "Sri Lanka Standard Time" },

        // UTC+05:45 Kathmandu
        { "Asia/Katmandu", "Nepal Standard Time" },

        // UTC+06:00 Astana
        { "Asia/Bishkek", "Central Asia Standard Time" },
        { "Antarctica/Vostok", "Central Asia Standard Time" },
        { "Asia/Urumqi", "Central Asia Standard Time" },
        { "Indian/Chagos", "Central Asia Standard Time" },
        { "Etc/GMT-6", "Central Asia Standard Time" },

        // UTC+06:00 Dhaka
        { "Asia/Dhaka", "Bangladesh Standard Time" },
        { "Asia/Thimphu", "Bangladesh Standard Time" },

        // UTC+06:00 Omsk
        { "Asia/Omsk", "Omsk Standard Time" },

        // UTC+06:30 Yangon (Rangoon)
        { "Asia/Rangoon", "Myanmar Standard Time" },
        { "Indian/Cocos", "Myanmar Standard Time" },

        // UTC+07:00 Bangkok, Hanoi, Jakarta
        { "Asia/Bangkok", "SE Asia Standard Time" },
        { "Antarctica/Davis", "SE Asia Standard Time" },
        { "Indian/Christmas", "SE Asia Standard Time" },
        { "Asia/Jakarta", "SE Asia Standard Time" },
        { "Asia/Pontianak", "SE Asia Standard Time" },
        { "Asia/Phnom_Penh", "SE Asia Standard Time" },
        { "Asia/Vientiane", "SE Asia Standard Time" },
        { "Asia/Saigon", "SE Asia Standard Time" },
        { "Etc/GMT-7", "SE Asia Standard Time" },

        // UTC+07:00 Barnaul, Gorno-Altaysk
        { "Asia/Barnaul", "Altai Standard Time" },

        // UTC+07:00 Hovd
        { "Asia/Hovd", "W. Mongolia Standard Time" },

        // UTC+07:00 Krasnoyarsk
        { "Asia/Krasnoyarsk", "North Asia Standard Time" },
        { "Asia/Novokuznetsk", "North Asia Standard Time" },

        // UTC+07:00 Novosibirsk
        { "Asia/Novosibirsk", "N. Central Asia Standard Time" },

        // UTC+07:00 Tomsk
        { "Asia/Tomsk", "Tomsk Standard Time" },

        // UTC+08:00 Beijing, Chongqing, Hong Kong, Urumqi
        { "Asia/Shanghai", "China Standard Time" },
        { "Asia/Hong_Kong", "China Standard Time" },
        { "Asia/Macau", "China Standard Time" },

        // UTC+08:00 Irkutsk
        { "Asia/Irkutsk", "North Asia East Standard Time" },

        // UTC+08:00 Kuala Lumpur, Singapore
        { "Asia/Singapore", "Singapore Standard Time" },
        { "Asia/Brunei", "Singapore Standard Time" },
        { "Asia/Makassar", "Singapore Standard Time" },
        { "Asia/Kuala_Lumpur", "Singapore Standard Time" },
        { "Asia/Kuching", "Singapore Standard Time" },
        { "Asia/Manila", "Singapore Standard Time" },
        { "Etc/GMT-8", "Singapore Standard Time" },

        // UTC+08:00 Perth
        { "Australia/Perth", "W. Australia Standard Time" },

        // UTC+08:00 Taipei
        { "Asia/Taipei", "Taipei Standard Time" },

        // UTC+08:00 Ulaanbaatar
        { "Asia/Ulaanbaatar", "Ulaanbaatar Standard Time" },

        // UTC+08:45 Eucla
        { "Australia/Eucla", "Aus Central W. Standard Time" },

        // UTC+09:00 Chita
        { "Asia/Chita", "Transbaikal Standard Time" },

        // UTC+09:00 Osaka, Sapporo, Tokyo
        { "Asia/Tokyo", "Tokyo Standard Time" },
        { "Asia/Jayapura", "Tokyo Standard Time" },
        { "Pacific/Palau", "Tokyo Standard Time" },
        { "Asia/Dili", "Tokyo Standard Time" },
        { "Etc/GMT-9", "Tokyo Standard Time" },

        // UTC+09:00 Pyongyang
        { "Asia/Pyongyang", "North Korea Standard Time" },

        // UTC+09:00 Seoul
        { "Asia/Seoul", "Korea Standard Time" },

        // UTC+09:00 Yakutsk
        { "Asia/Yakutsk", "Yakutsk Standard Time" },
        { "Asia/Khandyga", "Yakutsk Standard Time" },

        // UTC+09:30 Adelaide
        { "Australia/Adelaide", "Cen. Australia Standard Time" },
        { "Australia/Broken_Hill", "Cen. Australia Standard Time" },

        // UTC+09:30 Darwin
        { "Australia/Darwin", "AUS Central Standard Time" },

        // UTC+10:00 Brisbane
        { "Australia/Brisbane", "E. Australia Standard Time" },
        { "Australia/Lindeman", "E. Australia Standard Time" },

        // UTC+10:00 Canberra, Melbourne, Sydney
        { "Australia/Sydney", "AUS Eastern Standard Time" },
        { "Australia/Melbourne", "AUS Eastern Standard Time" },

        // UTC+10:00 Guam, Port Moresby
        { "Pacific/Port_Moresby", "West Pacific Standard Time" },
        { "Antarctica/DumontDUrville", "West Pacific Standard Time" },
        { "Pacific/Truk", "West Pacific Standard Time" },
        { "Pacific/Guam", "West Pacific Standard Time" },
        { "Pacific/Saipan", "West Pacific Standard Time" },
        { "Etc/GMT-10", "West Pacific Standard Time" },

        // UTC+10:00 Hobart
        { "Australia/Hobart", "Tasmania Standard Time" },
        { "Antarctica/Macquarie", "Tasmania Standard Time" },

        // UTC+10:00 Vladivostok
        { "Asia/Vladivostok", "Vladivostok Standard Time" },
        { "Asia/Ust-Nera", "Vladivostok Standard Time" },

        // UTC+10:30 Lord Howe Island
        { "Australia/Lord_Howe", "Lord Howe Standard Time" },

        // UTC+11:00 Bougainville Island
        { "Pacific/Bougainville", "Bougainville Standard Time" },

        // UTC+11:00 Chokurdakh
        { "Asia/Srednekolymsk", "Russia Time Zone 10" },

        // UTC+11:00 Magadan
        { "Asia/Magadan", "Magadan Standard Time" },

        // UTC+11:00 Norfolk Island
        { "Pacific/Norfolk", "Norfolk Standard Time" },

        // UTC+11:00 Sakhalin
        { "Asia/Sakhalin", "Sakhalin Standard Time" },

        // UTC+11:00 Solomon Is., New Caledonia
        { "Pacific/Guadalcanal", "Central Pacific Standard Time" },
        { "Antarctica/Casey", "Central Pacific Standard Time" },
        { "Pacific/Ponape", "Central Pacific Standard Time" },
        { "Pacific/Kosrae", "Central Pacific Standard Time" },
        { "Pacific/Noumea", "Central Pacific Standard Time" },
        { "Pacific/Efate", "Central Pacific Standard Time" },
        { "Etc/GMT-11", "Central Pacific Standard Time" },

        // UTC+12:00 Anadyr, Petropavlovsk-Kamchatsky
        { "Asia/Kamchatka", "Russia Time Zone 11" },
        { "Asia/Anadyr", "Russia Time Zone 11" },

        // UTC+12:00 Auckland, Wellington
        { "Pacific/Auckland", "New Zealand Standard Time" },
        { "Antarctica/McMurdo", "New Zealand Standard Time" },

        // UTC+12:00 Coordinated Universal Time+12
        { "Etc/GMT-12", "UTC+12" },
        { "Pacific/Tarawa", "UTC+12" },
        { "Pacific/Majuro", "UTC+12" },
        { "Pacific/Kwajalein", "UTC+12" },
        { "Pacific/Nauru", "UTC+12" },
        { "Pacific/Funafuti", "UTC+12" },
        { "Pacific/Wake", "UTC+12" },
        { "Pacific/Wallis", "UTC+12" },

        // UTC+12:00 Fiji
        { "Pacific/Fiji", "Fiji Standard Time" },

        // UTC+12:45 Chatham Islands
        { "Pacific/Chatham", "Chatham Islands Standard Time" },

        // UTC+13:00 Coordinated Universal Time+13
        { "Etc/GMT-13", "UTC+13" },
        { "Pacific/Enderbury", "UTC+13" },
        { "Pacific/Fakaofo", "UTC+13" },

        // UTC+13:00 Nuku'alofa
        { "Pacific/Tongatapu", "Tonga Standard Time" },

        // UTC+13:00 Samoa
        { "Pacific/Apia", "Samoa Standard Time" },

        // UTC+14:00 Kiritimati Island
        { "Pacific/Kiritimati", "Line Islands Standard Time" },
        { "Etc/GMT-14", "Line Islands Standard Time" }
    };

    /// <summary>
    /// Converts the <see cref="DateTime"/> to the specified time zone.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> to convert.</param>
    /// <param name="destinationTimeZone">The destination <see cref="TimeZoneInfo"/>.</param>
    /// <returns>
    /// A <see cref="DateTime"/> converted to the specified time zone.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="destinationTimeZone"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="dateTime"/> has an invalid <see cref="DateTimeKind"/>.</exception>
    public static DateTime ToTimeZone(this DateTime dateTime, TimeZoneInfo destinationTimeZone)
    {
        if (destinationTimeZone == null)
            throw new ArgumentNullException(nameof(destinationTimeZone));

        // If the DateTime is already in the destination time zone, return as-is
        if (dateTime.Kind == DateTimeKind.Local && destinationTimeZone.Equals(TimeZoneInfo.Local))
            return dateTime;

        // Convert based on the DateTime's Kind
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(dateTime, destinationTimeZone),
            DateTimeKind.Local => TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, destinationTimeZone),
            DateTimeKind.Unspecified => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc), destinationTimeZone),// Assume it's UTC for unspecified DateTimes
            _ => throw new ArgumentException("Invalid DateTimeKind", nameof(dateTime)),
        };
    }

    /// <summary>
    /// Converts the <see cref="DateTime"/> to the specified time zone using a time zone ID or common abbreviation.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> to convert.</param>
    /// <param name="destinationTimeZoneId">
    /// The destination time zone ID or abbreviation (e.g., "EST", "PST", "Eastern Standard Time").
    /// </param>
    /// <returns>
    /// A <see cref="DateTime"/> converted to the specified time zone.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="destinationTimeZoneId"/> is null or empty.</exception>
    /// <exception cref="TimeZoneNotFoundException">Thrown if the time zone ID is not found.</exception>
    public static DateTime ToTimeZone(this DateTime dateTime, string destinationTimeZoneId)
    {
        if (string.IsNullOrEmpty(destinationTimeZoneId))
            throw new ArgumentException("Time zone ID cannot be null or empty", nameof(destinationTimeZoneId));

        // Check if it's a common abbreviation first
        if (TimeZoneAbbreviations.TryGetValue(destinationTimeZoneId, out var actualTimeZoneId))
            destinationTimeZoneId = actualTimeZoneId;

        if (string.IsNullOrEmpty(actualTimeZoneId) && IanaToWindowsTimeZone.TryGetValue(destinationTimeZoneId, out actualTimeZoneId))
            destinationTimeZoneId = actualTimeZoneId;

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(destinationTimeZoneId);
            return dateTime.ToTimeZone(timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            // If the time zone is not found, return the original DateTime
            return dateTime;
        }
    }
}
