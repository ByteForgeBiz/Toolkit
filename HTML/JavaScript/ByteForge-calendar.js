/*
 *  ___      _       ___                                 _             _            _    
 * | _ )_  _| |_ ___| __|__ _ _ __ _ ___   ___   __ __ _| |___ _ _  __| |__ _ _ _  (_)___
 * | _ \ || |  _/ -_) _/ _ \ '_/ _` / -_) |___| / _/ _` | / -_) ' \/ _` / _` | '_| | (_-<
 * |___/\_, |\__\___|_|\___/_| \__, \___|       \__\__,_|_\___|_||_\__,_\__,_|_|(_)/ /__/
 *      |__/                   |___/                                             |__/    
 */

/**
 * Custom Calendar Widget for Date Selection
 * 
 * A lightweight, customizable calendar widget that provides date picker functionality
 * without relying on HTML5 date inputs (which have limited styling options).
 * 
 * Features:
 * - Custom styling and theming
 * - Month/year navigation
 * - Click-outside-to-close behavior
 * - Keyboard accessible
 * - Cross-browser compatible
 * - Integrates with standard input fields
 * 
 * Usage:
 * <input type="text" id="txtStartDate" readonly>
 * <button onclick="showDatePicker('txtStartDate')">📅</button>
 * 
 * @module CalendarWidget
 */

/**
 * Shows the date picker near the specified input element.
 * @param {string} inputId - The ID of the input element.
 */
function showDatePicker(inputId) {
    var input = document.getElementById(inputId);
    var picker = document.getElementById('customDatePicker');
        
    if (!picker) {
        // Create the date picker if it doesn't exist
        createDatePicker();
        picker = document.getElementById('customDatePicker');
    }

    // Position the picker near the input
    var rect = input.getBoundingClientRect();
    picker.style.top = (rect.bottom + window.scrollY) + 'px';
    picker.style.left = rect.left + 'px';
        
    // Show the picker
    picker.style.display = 'block';
        
    // Update the picker with current date or input value
    updateDatePicker(input.value);
}

/**
 * Creates the date picker element and appends it to the document body.
 */
function createDatePicker() {
    var div = document.createElement('div');
    div.id = 'customDatePicker';
    div.className = 'bf-date-picker';
    div.style.position = 'absolute';
    div.style.display = 'none';
    div.style.zIndex = '1000';
        
    var html = '<div class="bf-date-picker-header">';
    html += '<select id="monthSelect" onchange="updateCalendarDays()" class="bf-date-select">';
    var months = ['January', 'February', 'March', 'April', 'May', 'June', 
                    'July', 'August', 'September', 'October', 'November', 'December'];
    for (var i = 0; i < 12; i++) {
        html += '<option value="' + i + '">' + months[i] + '</option>';
    }
    html += '</select>';
        
    html += '<select id="yearSelect" onchange="updateCalendarDays()" class="bf-date-select">';
    var currentYear = new Date().getFullYear();
    for (var y = currentYear - 10; y <= currentYear + 10; y++) {
        html += '<option value="' + y + '">' + y + '</option>';
    }
    html += '</select></div>';
        
    html += '<table class="bf-calendar-table">';
    html += '<thead><tr><th>Su</th><th>Mo</th><th>Tu</th><th>We</th><th>Th</th><th>Fr</th><th>Sa</th></tr></thead>';
    html += '<tbody id="calendarBody"></tbody></table>';
        
    div.innerHTML = html;
    document.body.appendChild(div);
        
    // Close picker when clicking outside
    document.addEventListener('click', function (e) {
        var btn = document.getElementById('calendarButton');
        if (!div.contains(e.target) && (!btn || !btn.contains(e.target))) {
            div.style.display = 'none';
        }
    });
}

/**
 * Updates the calendar days based on the selected month and year.
 */
function updateCalendarDays() {
    var month = parseInt(document.getElementById('monthSelect').value);
    var year = parseInt(document.getElementById('yearSelect').value);
    var tbody = document.getElementById('calendarBody');
        
    var firstDay = new Date(year, month, 1);
    var lastDay = new Date(year, month + 1, 0);
    var today = new Date();
    var date = 1;
    var html = '';
        
    for (var i = 0; i < 6; i++) {
        html += '<tr>';
        for (var j = 0; j < 7; j++) {
            if (i === 0 && j < firstDay.getDay()) {
                html += '<td class="bf-calendar-empty"></td>';
            } else if (date > lastDay.getDate()) {
                html += '<td class="bf-calendar-empty"></td>';
            } else {
                var isToday = (year === today.getFullYear() && 
                              month === today.getMonth() && 
                              date === today.getDate());
                var cellClass = 'bf-calendar-date' + (isToday ? ' bf-calendar-today' : '');
                
                html += '<td onclick="selectDate(' + year + ',' + month + ',' + date + 
                        ')" class="' + cellClass + '">' + 
                        date + '</td>';
                date++;
            }
        }
        html += '</tr>';
        if (date > lastDay.getDate()) break;
    }
        
    tbody.innerHTML = html;
}

/**
 * Selects a date and updates the input value.
 * @param {number} year - The selected year.
 * @param {number} month - The selected month (0-based).
 * @param {number} day - The selected day.
 */
function selectDate(year, month, day) {
    var date = new Date(year, month, day);
    
    // Find the currently focused input or default to txtStartDate
    var activeInput = document.activeElement;
    var input = null;
    
    if (activeInput && activeInput.tagName === 'INPUT' && activeInput.type === 'text') {
        input = activeInput;
    } else {
        input = document.getElementById('txtStartDate');
    }
    
    if (input) {
        input.setAttribute('manualUpdate', 'true');
        input.value = (month + 1).toString().padStart(2, '0') + '/' + 
                        day.toString().padStart(2, '0') + '/' + year;
        
        // Trigger change event
        var event = new Event('change', { bubbles: true });
        input.dispatchEvent(event);
    }
        
    document.getElementById('customDatePicker').style.display = 'none';
        
    // Try to move focus to next logical field
    var agentNotes = document.getElementById('txtAgentNotes');
    if (agentNotes) {
        agentNotes.focus();
    } else if (input) {
        // Focus next input field if available
        var allInputs = Array.from(document.querySelectorAll('input[type="text"], textarea, select'));
        var currentIndex = allInputs.indexOf(input);
        if (currentIndex >= 0 && currentIndex < allInputs.length - 1) {
            allInputs[currentIndex + 1].focus();
        }
    }
}

/**
 * Updates the date picker with the specified date string.
 * @param {string} dateStr - The date string to update the picker with.
 */
function updateDatePicker(dateStr) {
    var dt;
    
    if (dateStr && dateStr.trim()) {
        // Try to parse common date formats
        if (dateStr.includes('/')) {
            dt = new Date(dateStr);
        } else if (dateStr.includes('-')) {
            dt = new Date(dateStr);
        } else {
            dt = new Date();
        }
        
        // Validate parsed date
        if (isNaN(dt.getTime())) {
            dt = new Date();
        }
    } else {
        dt = new Date();
    }
    
    var monthSelect = document.getElementById('monthSelect');
    var yearSelect = document.getElementById('yearSelect');
        
    if (monthSelect && yearSelect) {
        monthSelect.value = dt.getMonth();
        yearSelect.value = dt.getFullYear();
        updateCalendarDays();
    }
}

/**
 * Creates a calendar button for the specified input field.
 * @param {string} inputId - The ID of the input field
 * @param {string} buttonText - The text/symbol for the button (default: 📅)
 * @returns {HTMLElement} The created button element
 */
function createCalendarButton(inputId, buttonText = '📅') {
    var button = document.createElement('button');
    button.type = 'button';
    button.className = 'bf-calendar-button';
    button.textContent = buttonText;
    button.setAttribute('aria-label', 'Open calendar');
    button.onclick = function() { showDatePicker(inputId); };
    
    return button;
}

/**
 * Initializes calendar functionality for all date inputs with the data-calendar attribute.
 * Call this function when the DOM is ready.
 */
function initializeDatePickers() {
    var dateInputs = document.querySelectorAll('input[data-calendar="true"]');
    
    dateInputs.forEach(function(input) {
        // Make input readonly to prevent manual typing
        input.readOnly = true;
        
        // Create and insert calendar button
        var button = createCalendarButton(input.id);
        input.parentNode.insertBefore(button, input.nextSibling);
        
        // Add CSS class for styling
        input.classList.add('bf-date-input');
    });
}

// Auto-initialize when DOM is loaded
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeDatePickers);
} else {
    initializeDatePickers();
}