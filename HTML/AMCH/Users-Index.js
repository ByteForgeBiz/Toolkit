(function () {
    'use strict';

    var antiForgeryToken = $('input[name="__RequestVerificationToken"]').first().val();
    var config = window.amchUsersIndex || {};
    var currentUserId = config.currentUserId || 0;
    var users = [];
    var roles = [];
    var clients = [];
    var userSearch = '';
    var pageSize = 20;
    var currentPage = 1;
    var sortField = '';
    var sortDirection = '';
    var isClientScopedEditor = false;
    var editActionText = '<span aria-hidden="true">&#9999;&#65039;</span> Edit';
    var deleteActionText = '<span aria-hidden="true">&#10060;</span> Delete';
    var passwordActionText = '<span aria-hidden="true">&#128272;</span> Password';
    var invitationActionText = '<span aria-hidden="true">&#128232;</span> Invitation';
    var internalClientLabel = 'Internal User - Telecom';
    var rolePickerOptions = {
        classes: {
            option: 'admin-role-picker-option',
            name: 'admin-role-picker-option-name',
            description: 'admin-role-picker-option-description'
        }
    };

    loadEditorData();

    $('#addUserBtn').on('click', function () { openUserModal(null); });
    $('#userModalClose, #userModalCancelBtn').on('click', closeUserModal);
    $('#resetModalClose, #resetModalCancelBtn').on('click', closeResetModal);
    $('#userModalSaveBtn').on('click', saveUser);
    $('#resetModalSaveBtn').on('click', resetPassword);
    $('#uSendInvitation').on('change', syncInvitationFields);
    $('#uRolesWrap').on('input', '#uRoleSearch', renderRoleLists);
    $('#uRolesWrap').on('click', '.admin-role-picker-option', function () { ByteForgeMultiSelector.toggleOption(this); });
    $('#uRolesWrap').on('keydown', '.admin-role-picker-option', ByteForgeMultiSelector.handleOptionKeydown);
    $('#uRolesWrap').on('dblclick', '#uAvailableRoles .admin-role-picker-option', function () { moveRoleIds([parseRoleId($(this))], true); });
    $('#uRolesWrap').on('dblclick', '#uSelectedRoles .admin-role-picker-option', function () { moveRoleIds([parseRoleId($(this))], false); });
    $('#uRolesWrap').on('click', '#uRoleAddBtn', function () { moveRoles('#uAvailableRoles .admin-role-picker-option.is-selected', true); });
    $('#uRolesWrap').on('click', '#uRoleAddAllBtn', function () { moveRoles('#uAvailableRoles .admin-role-picker-option', true); });
    $('#uRolesWrap').on('click', '#uRoleRemoveBtn', function () { moveRoles('#uSelectedRoles .admin-role-picker-option.is-selected', false); });
    $('#uRolesWrap').on('click', '#uRoleRemoveAllBtn', function () { moveRoles('#uSelectedRoles .admin-role-picker-option', false); });
    $('#uClientsWrap').on('input', '#uClientSearch', function () {
        renderClientOptions();
        openClientDropdown();
    });
    $('#uClientsWrap').on('focus', '#uClientSearch', openClientDropdown);
    $('#uClientsWrap').on('keydown', '#uClientSearch', onClientSearchKeydown);
    $('#uClientsWrap').on('click', '#uClientToggleBtn', toggleClientDropdown);
    $('#uClientsWrap').on('click', '#uClientClearBtn', function () {
        selectClient(0);
    });
    $(document).on('click', '#uClientDropdown .admin-client-combo-item', function () {
        selectClient(parseInt($(this).data('id'), 10) || 0);
    });
    $('#userSearch').on('input', function () {
        userSearch = String($(this).val() || '');
        currentPage = 1;
        renderUsers();
    });

    $('#usersTableWrap').on('click', '.btn-edit-user', function () {
        var user = findUser(parseInt($(this).data('id'), 10));
        if (!user) return;
        if (isCurrentContextRow(user)) {
            window.location.href = config.profileUrl || '/Profile';
            return;
        }

        openUserModal(user);
    });

    $('#usersTableWrap').on('click', '.btn-reset-password', function () {
        var user = findUser(parseInt($(this).data('id'), 10));
        if (!user) return;
        if (isCurrentContextRow(user)) {
            window.location.href = config.profileUrl || '/Profile';
            return;
        }

        $('#rpId').val(user.id);
        $('#rpPassword, #rpConfirmPassword').val('');
        $('#resetModalError').text('');
        AMCHAdminMessages.showModal('#resetPasswordModal', { maxWidth: '520px', initialFocusSelector: '#rpPassword' });
    });

    $('#usersTableWrap').on('click', '.btn-delete-user', function () {
        var user = findUser(parseInt($(this).data('id'), 10));
        if (!user) return;
        AMCHAdminMessages.confirm(
            'Delete user "' + user.username + '"?',
            function () { deleteUser(user.id); },
            'Delete User',
            'Delete',
            'Cancel');
    });
    $(document).on('click', function (event) {
        if (!$(event.target).closest('#uClientsWrap, #uClientDropdown').length) {
            closeClientDropdown();
        }
    });

    $('#usersTableWrap').on('click', '.btn-invite-user', function () {
        var user = findUser(parseInt($(this).data('id'), 10));
        if (!user) return;
        inviteUser(user.id, $(this));
    });
    $('#usersTableWrap').on('click', '.btn-user-page', function () {
        var page = parseInt($(this).data('page'), 10);
        if (!page || page === currentPage) return;
        currentPage = page;
        renderUsers();
    });
    $('#usersTableWrap').on('change', '.user-page-size', function () {
        pageSize = parseInt($(this).val(), 10) || 20;
        currentPage = 1;
        renderUsers();
    });
    $('#usersTableWrap').on('click', '.sortable-header', function () {
        var field = String($(this).data('field') || '');
        if (!field) return;
        var next = AMCHAdminListTools.nextSort(sortField, sortDirection, field);
        sortField = next.field;
        sortDirection = next.direction;
        currentPage = 1;
        renderUsers();
    });

    function loadEditorData() {
        $('#usersTableWrap').html('Loading users...');
        $.when(
            $.getJSON(config.listUsersUrl),
            $.getJSON(config.listRolesUrl),
            $.getJSON(config.listClientsUrl)
        ).done(function (usersResult, rolesResult, clientsResult) {
            users = usersResult[0] || [];
            roles = (rolesResult[0] || []).filter(function (role) { return role.isActive; });
            clients = (clientsResult[0] || []).filter(function (client) { return client.isActive; });
            isClientScopedEditor = config.isClientScopedEditor === true;
            $('#uClientField').toggle(!isClientScopedEditor);
            renderUsers();
            renderRolePicker([]);
            renderClientPicker([]);
        }).fail(function () {
            $('#usersTableWrap').html('Failed to load account data.');
            $('#userCountPill').text('Error');
            $('#uRolesWrap').html('Failed to load roles.');
        });
    }

    function renderUsers() {
        var filteredUsers = getFilteredUsers();
        $('#userCountPill').text(userSearch ? filteredUsers.length + ' of ' + users.length : users.length + ' total');
        if (!filteredUsers.length) {
            $('#usersTableWrap').html('<div class="admin-empty-state">' + (userSearch ? 'No users match the current search.' : getEmptyUsersMessage()) + '</div>');
            return;
        }

        var totalPages = Math.max(1, Math.ceil(filteredUsers.length / pageSize));
        if (currentPage > totalPages) currentPage = totalPages;

        var sortedUsers = AMCHAdminListTools.sortRows(filteredUsers, sortField, sortDirection, getUserSortValue);
        var startIndex = (currentPage - 1) * pageSize;
        var pageUsers = sortedUsers.slice(startIndex, startIndex + pageSize);
        var topPagination = renderPagination(totalPages, startIndex, pageUsers.length, filteredUsers.length, true);
        var html = topPagination + '<div class="table-responsive"><table class="table admin-table-zero">' +
            '<thead><tr>' +
            buildSortableHeader('Username', 'username') +
            buildSortableHeader('Email', 'email') +
            buildSortableHeader('Display Name', 'displayName') +
            buildSortableHeader('Roles', 'roles') +
            buildSortableHeader('Clients', 'clientsSummary') +
            buildSortableHeader('Last Login', 'lastLoginAtUtc') +
            buildSortableHeader('Active', 'isActive') +
            '<th class="admin-th-actions-xl admin-cell-center">Actions</th>' +
            '</tr></thead><tbody>';

        pageUsers.forEach(function (user) {
            var canManage = user.canManage !== false;
            var isCurrentContext = isCurrentContextRow(user);
            var editPasswordDisabledAttr = canManage || isCurrentContext ? '' : ' disabled';
            var inviteButton = user.hasActiveInvitation
                ? '<button type="button" class="btn btn-secondary btn-invite-user admin-btn-compact admin-btn-gap" data-id="' + user.id + '">' + invitationActionText + '</button>'
                : '<button type="button" class="btn btn-secondary admin-btn-compact admin-btn-gap admin-action-placeholder" tabindex="-1" aria-hidden="true">' + invitationActionText + '</button>';
            if (!canManage) {
                inviteButton = '<button type="button" class="btn btn-secondary admin-btn-compact admin-btn-gap admin-action-placeholder" disabled>' + invitationActionText + '</button>';
            }
            var rolesSummary = (user.roles || []).length
                ? user.roles.map(function (roleName) { return escHtml(roleName); }).join(', ')
                : '<span class="admin-generated-muted">None</span>';
            html += '<tr>' +
                '<td><strong>' + escHtml(user.username) + '</strong>' + (user.id === currentUserId ? '<div class="admin-generated-muted-sm">Current session</div>' : '') + '</td>' +
                '<td>' + escHtml(user.email) + '</td>' +
                '<td>' + escHtml(user.displayName) + '</td>' +
                '<td>' + rolesSummary + '</td>' +
                '<td>' + escHtml(user.clientsSummary || 'Internal') + '</td>' +
                '<td>' + formatUtc(user.lastLoginAtUtc) + '</td>' +
                '<td>' + (user.isActive ? '<span class="admin-pill success">Active</span>' : '<span class="admin-pill warning">Inactive</span>') + '</td>' +
                '<td class="admin-cell-actions">' +
                '<button type="button" class="btn btn-secondary btn-edit-user admin-btn-compact admin-btn-gap" data-id="' + user.id + '"' + editPasswordDisabledAttr + '>' + editActionText + '</button>' +
                '<button type="button" class="btn btn-secondary btn-reset-password admin-btn-compact admin-btn-gap" data-id="' + user.id + '"' + editPasswordDisabledAttr + '>' + passwordActionText + '</button>' +
                inviteButton +
                '<button type="button" class="btn btn-outline-danger btn-delete-user admin-btn-compact" data-id="' + user.id + '" ' + (!canManage || user.id === currentUserId ? 'disabled' : '') + '>' + deleteActionText + '</button>' +
                '</td>' +
                '</tr>';
        });

        html += '</tbody></table></div>';
        if (filteredUsers.length >= 50) {
            html += renderPagination(totalPages, startIndex, pageUsers.length, filteredUsers.length, false);
        }
        $('#usersTableWrap').html(html);
    }

    /**
     * Gets the empty-state message for the current user-management scope.
     * @returns {string} The empty users message.
     */
    function getEmptyUsersMessage() {
        return isClientScopedEditor
            ? 'No client users are configured yet. Use Add User to create the first client portal or API user.'
            : 'No users are configured yet.';
    }

    /**
     * Determines whether the row is the signed-in user shown outside the client-user management scope.
     * @param {Object} user The user row.
     * @returns {boolean} True when the row should use self-service profile actions.
     */
    function isCurrentContextRow(user) {
        return user && user.id === currentUserId && user.canManage === false;
    }

    /**
     * Returns users matching the current tokenized search.
     * @returns {Array} The visible users.
     */
    function getFilteredUsers() {
        return AMCHAdminListTools.filterRows(users, userSearch, function (user) {
            return [
                user.username,
                user.email,
                user.displayName,
                (user.roles || []).join(' '),
                user.clientsSummary || 'Internal',
                user.isActive ? 'active yes' : 'inactive no',
                user.hasActiveInvitation ? 'invited invitation pending' : '',
                user.lastLoginAtUtc || ''
            ].join(' ');
        });
    }

    /**
     * Gets a sortable user value.
     * @param {Object} user The user row.
     * @param {string} field The sort field.
     * @returns {*} The sortable value.
     */
    function getUserSortValue(user, field) {
        if (field === 'roles') return (user.roles || []).join(', ');
        if (field === 'isActive') return user.isActive ? 1 : 0;
        return user[field] || '';
    }

    /**
     * Renders the two-list role picker.
     * @param {number[]} selectedRoleIds Role identifiers currently assigned to the user.
     */
    function renderRolePicker(selectedRoleIds) {
        selectedRoleIds = selectedRoleIds || [];
        if (!roles.length) {
            $('#uRolesWrap').html('<span class="admin-error">No active roles are available.</span>');
            return;
        }

        var selectedMap = {};
        selectedRoleIds.forEach(function (id) { selectedMap[id] = true; });
        $('#uRolesWrap').data('selectedRoleIds', selectedMap);
        $('#uRolesWrap').html('<div class="admin-role-picker-toolbar">' +
            '<input type="text" id="uRoleSearch" class="form-control" maxlength="100" placeholder="Search roles..." />' +
            '</div>' +
            '<div class="admin-role-picker-grid">' +
            '<div><label class="admin-role-picker-label">Available Roles</label><div id="uAvailableRoles" class="admin-role-picker-list" role="listbox" aria-label="Available roles" aria-multiselectable="true"></div></div>' +
            '<div class="admin-role-picker-actions" aria-label="Move roles">' +
            '<button type="button" class="btn btn-secondary" id="uRoleAddBtn" title="Add selected">&gt;</button>' +
            '<button type="button" class="btn btn-secondary" id="uRoleAddAllBtn" title="Add all">&gt;&gt;</button>' +
            '<button type="button" class="btn btn-secondary" id="uRoleRemoveBtn" title="Remove selected">&lt;</button>' +
            '<button type="button" class="btn btn-secondary" id="uRoleRemoveAllBtn" title="Remove all">&lt;&lt;</button>' +
            '</div>' +
            '<div><label class="admin-role-picker-label">Selected Roles</label><div id="uSelectedRoles" class="admin-role-picker-list" role="listbox" aria-label="Selected roles" aria-multiselectable="true"></div></div>' +
            '</div>');
        renderRoleLists();
    }

    /**
     * Renders the single-client searchable dropdown.
     * @param {number[]} selectedClientIds Client identifiers currently assigned to the user.
     */
    function renderClientPicker(selectedClientIds) {
        selectedClientIds = selectedClientIds || [];
        $('#uClientsWrap').data('selectedClientId', parseInt(selectedClientIds[0], 10) || 0);
        $('#uClientsWrap').html(
            '<div class="admin-client-combo">' +
            '<div class="admin-client-combo-wrap">' +
            '<input type="text" id="uClientSearch" class="form-control admin-client-search" maxlength="200" autocomplete="off" placeholder="' + internalClientLabel + '" aria-label="Client assignment" />' +
            '<input type="hidden" id="uClientId" value="0" />' +
            '<button type="button" id="uClientToggleBtn" class="admin-client-combo-toggle" tabindex="-1" title="Show clients">&#9660;</button>' +
            '<button type="button" id="uClientClearBtn" class="admin-client-combo-clear" tabindex="-1" title="' + internalClientLabel + '">&#10005;</button>' +
            '</div>' +
            '</div>');
        ensureClientDropdown();
        syncSelectedClientDisplay();
        renderClientOptions();
    }

    /**
     * Ensures the client dropdown is portaled outside modal containers.
     */
    function ensureClientDropdown() {
        if (!$('#uClientDropdown').length) {
            $('body').append('<div id="uClientDropdown" class="admin-client-combo-dropdown"></div>');
        }
    }

    /**
     * Renders the filtered client dropdown options.
     */
    function renderClientOptions() {
        var selectedClientId = parseInt($('#uClientsWrap').data('selectedClientId'), 10) || 0;
        var query = String($('#uClientSearch').val() || '').toLowerCase().trim();
        var visibleClients = clients.filter(function (client) {
            if (!query) return true;
            return AMCHAdminListTools.matches(query, [
                client.clientCode,
                client.displayName
            ].join(' '));
        });
        var html = '<button type="button" class="admin-client-combo-item' + (selectedClientId === 0 ? ' is-selected' : '') + '" data-id="0">' +
            '<strong>Internal User</strong>' +
            '<small>Telecom</small>' +
            '</button>';

        visibleClients.forEach(function (client) {
            var id = parseInt(client.id, 10) || 0;
            if (!id) return;
            html += '<button type="button" class="admin-client-combo-item' + (id === selectedClientId ? ' is-selected' : '') + '" data-id="' + id + '">' +
                '<strong>' + escHtml(client.clientCode) + '</strong>' +
                '<small>' + escHtml(client.displayName || client.clientCode) + '</small>' +
                '</button>';
        });

        if (!visibleClients.length && query) {
            html += '<button type="button" class="admin-client-combo-item" disabled>No matching clients</button>';
        }

        $('#uClientDropdown').html(html);
    }

    /**
     * Selects the supplied client identifier.
     * @param {number} clientId The client identifier, or zero for internal.
     */
    function selectClient(clientId) {
        $('#uClientsWrap').data('selectedClientId', clientId || 0);
        syncSelectedClientDisplay();
        renderClientOptions();
        closeClientDropdown();
    }

    /**
     * Syncs the combo input and hidden value to the selected client.
     */
    function syncSelectedClientDisplay() {
        var selectedClientId = parseInt($('#uClientsWrap').data('selectedClientId'), 10) || 0;
        var selectedClient = selectedClientId > 0 ? findClient(selectedClientId) : null;
        $('#uClientId').val(selectedClientId);
        $('#uClientSearch').val(selectedClient ? selectedClient.clientCode + ' - ' + (selectedClient.displayName || selectedClient.clientCode) : internalClientLabel);
        $('#uClientClearBtn').toggleClass('admin-hidden', selectedClientId === 0);
    }

    /**
     * Opens the client dropdown.
     */
    function openClientDropdown() {
        if ((parseInt($('#uClientsWrap').data('selectedClientId'), 10) || 0) === 0 &&
            String($('#uClientSearch').val() || '') === internalClientLabel) {
            $('#uClientSearch').val('');
        }

        renderClientOptions();
        positionClientDropdown();
        $('#uClientDropdown').addClass('open');
        $(window).on('resize.clientDropdown scroll.clientDropdown', positionClientDropdown);
        $('#userModal .admin-modal-body').on('scroll.clientDropdown', positionClientDropdown);
    }

    /**
     * Positions the portaled client dropdown below the combo input.
     */
    function positionClientDropdown() {
        var combo = $('#uClientsWrap .admin-client-combo-wrap').get(0);
        if (!combo || !$('#uClientDropdown').length) return;

        var bounds = combo.getBoundingClientRect();
        $('#uClientDropdown').css({
            left: Math.round(bounds.left) + 'px',
            top: Math.round(bounds.bottom + 3) + 'px',
            width: Math.round(bounds.width) + 'px'
        });
    }

    /**
     * Closes the client dropdown.
     */
    function closeClientDropdown() {
        $('#uClientDropdown').removeClass('open');
        $(window).off('.clientDropdown');
        $('#userModal .admin-modal-body').off('.clientDropdown');
        syncSelectedClientDisplay();
    }

    /**
     * Toggles the client dropdown.
     */
    function toggleClientDropdown() {
        if ($('#uClientDropdown').hasClass('open')) {
            closeClientDropdown();
        } else {
            openClientDropdown();
            $('#uClientSearch').focus().select();
        }
    }

    /**
     * Handles keyboard shortcuts for the client combo.
     * @param {KeyboardEvent} event The keyboard event.
     */
    function onClientSearchKeydown(event) {
        if (event.key === 'Escape') {
            closeClientDropdown();
            return;
        }

        if (event.key === 'Enter') {
            var $first = $('#uClientDropdown .admin-client-combo-item:not(:disabled)').first();
            if ($first.length) {
                event.preventDefault();
                selectClient(parseInt($first.data('id'), 10) || 0);
            }
        }
    }

    /**
     * Rebuilds the visible role lists using the current selected roles and search text.
     */
    function renderRoleLists() {
        var selectedRoleIds = $('#uRolesWrap').data('selectedRoleIds') || {};
        var query = ($('#uRoleSearch').val() || '').toLowerCase().trim();
        var lists = ByteForgeMultiSelector.renderLists({
            items: roles,
            selected: selectedRoleIds,
            query: query,
            getValue: function (role) {
                return parseInt(role.id, 10) || 0;
            },
            matchesQuery: function (role, searchText) {
                return AMCHAdminListTools.matches(searchText, role.name + ' ' + (role.description || ''));
            },
            formatOption: function (role) {
                return formatRoleOption(role);
            }
        });

        $('#uAvailableRoles').html(lists.availableHtml);
        $('#uSelectedRoles').html(lists.selectedHtml);
    }

    /**
     * Formats a role option with the database role name and description.
     * @param {Object} role Role JSON returned by the roles endpoint.
     * @returns {string} HTML markup for the role picker option.
     */
    function formatRoleOption(role) {
        var description = role.description || '';
        return ByteForgeMultiSelector.formatOption(parseInt(role.id, 10) || 0, role.name, description, description, 'data-role-id', rolePickerOptions);
    }

    /**
     * Moves role options between the available and selected lists.
     * @param {string} selector Selector identifying role options to move.
     * @param {boolean} add True to select roles; false to remove them.
     */
    function moveRoles(selector, add) {
        var roleIds = ByteForgeMultiSelector.getValues(selector, parseRoleId, rolePickerOptions);
        moveRoleIds(roleIds, add);
    }

    /**
     * Moves the specified role identifiers between selected and available state.
     * @param {number[]} roleIds Role identifiers to move.
     * @param {boolean} add True to select roles; false to remove them.
     */
    function moveRoleIds(roleIds, add) {
        var selectedRoleIds = $('#uRolesWrap').data('selectedRoleIds') || {};
        roleIds.forEach(function (roleId) {
            if (!roleId) return;
            if (add) selectedRoleIds[roleId] = true;
            else delete selectedRoleIds[roleId];
        });
        $('#uRolesWrap').data('selectedRoleIds', selectedRoleIds);
        renderRoleLists();
    }

    /**
     * Parses the role identifier stored on a custom role option.
     * @param {jQuery} $option Role option element.
     * @returns {number} Parsed role identifier.
     */
    function parseRoleId($option) {
        return parseInt($option.attr('data-role-id'), 10) || 0;
    }

    /**
     * Builds a sortable table header cell.
     * @param {string} label The visible label.
     * @param {string} field The sort field.
     * @returns {string} The header HTML.
     */
    function buildSortableHeader(label, field) {
        var indicator = '';
        if (sortField === field) indicator = sortDirection === 'asc' ? ' ▲' : ' ▼';
        return '<th class="sortable-header admin-sortable-header" data-field="' + escHtml(field) + '">' + escHtml(label + indicator) + '</th>';
    }

    /**
     * Renders account pagination controls.
     * @param {number} totalPages The total page count.
     * @param {number} startIndex The zero-based start index.
     * @param {number} visibleCount The number of visible users.
     * @param {number} totalCount The total filtered user count.
     * @param {boolean} includeSettings Whether to include the gear settings panel.
     * @returns {string} The pagination markup.
     */
    function renderPagination(totalPages, startIndex, visibleCount, totalCount, includeSettings) {
        return AMCHAdminListTools.renderPagination({
            totalPages: totalPages,
            startIndex: startIndex,
            pageCount: visibleCount,
            totalRows: totalCount,
            currentPage: currentPage,
            buttonClassName: 'btn-user-page',
            pageSize: pageSize,
            pageSizeClassName: 'user-page-size',
            includePageSize: includeSettings
        });
    }

    function openUserModal(user) {
        $('#userModalError').text('');
        if (user) {
            $('#userModalTitle').text('Edit User');
            $('#uId').val(user.id);
            $('#uUsername').val(user.username);
            $('#uDisplayName').val(user.displayName);
            $('#uEmail').val(user.email);
            $('#uIsActive').prop('checked', !!user.isActive);
            $('#uIsActive').prop('disabled', false);
            $('#createPasswordFields').hide();
            $('#inviteUserWrap').hide();
            $('#inviteUserHint').hide();
            $('#uSendInvitation').prop('checked', false);
            $('#uPassword, #uConfirmPassword').val('');
            renderRolePicker(mapRoleIds(user.roles || []));
            renderClientPicker(user.clientIds || []);
        } else {
            $('#userModalTitle').text('Add User');
            $('#uId').val(0);
            $('#uUsername, #uDisplayName, #uEmail, #uPassword, #uConfirmPassword').val('');
            $('#uIsActive').prop('checked', true);
            $('#uIsActive').prop('disabled', false);
            $('#inviteUserWrap').show();
            $('#inviteUserHint').show();
            $('#uSendInvitation').prop('checked', true);
            syncInvitationFields();
            renderRolePicker([]);
            renderClientPicker([]);
        }

        AMCHAdminMessages.showModal('#userModal', { maxWidth: '980px', initialFocusSelector: '#uUsername' });
    }

    function closeUserModal() {
        AMCHAdminMessages.closeModal('#userModal');
    }

    function closeResetModal() {
        AMCHAdminMessages.closeModal('#resetPasswordModal');
    }

    function saveUser() {
        var id = parseInt($('#uId').val(), 10) || 0;
        var password = $('#uPassword').val();
        var confirmPassword = $('#uConfirmPassword').val();
        var sendInvitation = id === 0 && $('#uSendInvitation').is(':checked');
        var selectedRoleIds = getSelectedRoleIds();

        if (!$('#uUsername').val().trim()) {
            $('#userModalError').text('Username is required.');
            return;
        }
        if (!$('#uDisplayName').val().trim()) {
            $('#userModalError').text('Display name is required.');
            return;
        }
        if (!$('#uEmail').val().trim()) {
            $('#userModalError').text('Email is required.');
            return;
        }
        if (!selectedRoleIds.length) {
            $('#userModalError').text('Select at least one role.');
            return;
        }
        if (id === 0 && !sendInvitation) {
            if (!password) {
                $('#userModalError').text('Password is required for new users.');
                return;
            }
            if (password !== confirmPassword) {
                $('#userModalError').text('Password confirmation does not match.');
                return;
            }
        }

        var payload = {
            __RequestVerificationToken: antiForgeryToken,
            Id: id,
            Username: $('#uUsername').val().trim(),
            DisplayName: $('#uDisplayName').val().trim(),
            Email: $('#uEmail').val().trim(),
            IsActive: $('#uIsActive').is(':checked'),
            Password: password,
            SendInvitation: sendInvitation,
            RoleIds: selectedRoleIds,
            ClientIds: getSelectedClientIds()
        };

        var $btn = $('#userModalSaveBtn');
        $btn.prop('disabled', true).text('Saving...');
        $('#userModalError').text('');

        $.ajax({
            url: config.saveUrl,
            type: 'POST',
            dataType: 'json',
            traditional: true,
            data: payload
        }).done(function (result) {
                $btn.prop('disabled', false).text('Save User');
                if (result.success) {
                    closeUserModal();
                    loadEditorData();
                } else {
                    $('#userModalError').text(result.error || 'Save failed.');
                }
            })
            .fail(function (xhr) {
                $btn.prop('disabled', false).text('Save User');
                $('#userModalError').text('Request failed (' + xhr.status + ').');
            });
    }

    function resetPassword() {
        var password = $('#rpPassword').val();
        var confirmPassword = $('#rpConfirmPassword').val();

        if (!password) {
            $('#resetModalError').text('Password is required.');
            return;
        }
        if (password !== confirmPassword) {
            $('#resetModalError').text('Password confirmation does not match.');
            return;
        }

        var $btn = $('#resetModalSaveBtn');
        $btn.prop('disabled', true).text('Saving...');
        $('#resetModalError').text('');

        $.post(config.resetPasswordUrl, {
            __RequestVerificationToken: antiForgeryToken,
            Id: parseInt($('#rpId').val(), 10) || 0,
            Password: password
        }).done(function (result) {
            $btn.prop('disabled', false).html('<span aria-hidden="true">&#128272;</span> Reset Password');
            if (result.success) {
                closeResetModal();
            } else {
                $('#resetModalError').text(result.error || 'Password reset failed.');
            }
        }).fail(function (xhr) {
            $btn.prop('disabled', false).html('<span aria-hidden="true">&#128272;</span> Reset Password');
            $('#resetModalError').text('Request failed (' + xhr.status + ').');
        });
    }

    function deleteUser(id) {
        $.post(config.deleteUrl, {
            __RequestVerificationToken: antiForgeryToken,
            id: id
        }).done(function (result) {
            if (result.success) {
                loadEditorData();
            } else {
                AMCHAdminMessages.error(result.error || 'Delete failed.', 'Delete User');
            }
        }).fail(function (xhr) {
            AMCHAdminMessages.error('Request failed (' + xhr.status + ').', 'Delete User');
        });
    }

    function inviteUser(id, $btn) {
        $btn = $btn || $();
        var originalText = $btn.text();
        $btn.prop('disabled', true).text('Sending...');

        $.post(config.inviteUrl, {
            __RequestVerificationToken: antiForgeryToken,
            id: id
        }).done(function (result) {
            $btn.prop('disabled', false).text(originalText);
            if (result.success) {
                loadEditorData();
            } else {
                AMCHAdminMessages.error(result.error || 'Invitation failed.', 'Invite User');
            }
        }).fail(function (xhr) {
            $btn.prop('disabled', false).text(originalText);
            AMCHAdminMessages.error('Request failed (' + xhr.status + ').', 'Invite User');
        });
    }

    function syncInvitationFields() {
        var sendInvitation = $('#uSendInvitation').is(':checked');
        if (sendInvitation) {
            $('#uPassword, #uConfirmPassword').val('');
            $('#createPasswordFields').hide();
            $('#uIsActive').prop('checked', true).prop('disabled', true);
        } else {
            $('#createPasswordFields').show();
            $('#uIsActive').prop('disabled', false);
        }
    }

    function mapRoleIds(roleNames) {
        var ids = [];
        roleNames = roleNames || [];
        roles.forEach(function (role) {
            if (roleNames.some(function (name) { return String(name).toLowerCase() === String(role.name).toLowerCase(); })) {
                ids.push(role.id);
            }
        });
        return ids;
    }

    /**
     * Gets selected role identifiers from the picker state.
     * @returns {number[]} Selected role identifiers.
     */
    function getSelectedRoleIds() {
        var selectedRoleIds = $('#uRolesWrap').data('selectedRoleIds') || {};
        return Object.keys(selectedRoleIds).map(function (id) {
            return parseInt(id, 10) || 0;
        }).filter(function (id) { return id > 0; });
    }

    /**
     * Gets the selected client identifier from the client dropdown.
     * @returns {number[]} Selected client identifiers.
     */
    function getSelectedClientIds() {
        if (isClientScopedEditor) {
            return [];
        }

        var id = parseInt($('#uClientId').val(), 10) || 0;
        return id > 0 ? [id] : [];
    }

    /**
     * Finds a client by identifier.
     * @param {number} id The client identifier.
     * @returns {Object|null} The matching client row.
     */
    function findClient(id) {
        for (var i = 0; i < clients.length; i++) {
            if ((parseInt(clients[i].id, 10) || 0) === id) return clients[i];
        }

        return null;
    }

    function findUser(id) {
        for (var i = 0; i < users.length; i++) {
            if (users[i].id === id) return users[i];
        }
        return null;
    }

    function formatUtc(value) {
        if (!value) return '<span class="admin-generated-muted">Never</span>';
        var date = new Date(value);
        if (isNaN(date.getTime())) return escHtml(String(value));
        return escHtml(date.getUTCFullYear() + '-' +
            pad(date.getUTCMonth() + 1) + '-' +
            pad(date.getUTCDate()) + ' ' +
            pad(date.getUTCHours()) + ':' +
            pad(date.getUTCMinutes()) + ':' +
            pad(date.getUTCSeconds()) + ' UTC');
    }

    function pad(value) {
        value = parseInt(value, 10) || 0;
        return value < 10 ? '0' + value : String(value);
    }

    function escHtml(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    /**
     * Escapes a value for safe use inside an HTML attribute.
     * @param {string} value Raw attribute value.
     * @returns {string} Escaped attribute value.
     */
    function escAttr(value) {
        return escHtml(value).replace(/\r?\n/g, '&#10;');
    }

})();
