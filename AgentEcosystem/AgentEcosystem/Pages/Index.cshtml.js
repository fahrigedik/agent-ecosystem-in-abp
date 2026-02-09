(function () {
    // ═══════════════════════════════════════════════════
    // AI Agent Ecosystem — Frontend Controller
    // ═══════════════════════════════════════════════════

    var $query = $('#researchQuery');
    var $btnStart = $('#btnStartResearch');
    var $resultsSection = $('#resultsSection');

    // ─── Pipeline Steps ───
    function resetPipeline() {
        $('#stepIdle').removeClass('d-none');
        $('#stepSearch, #stepResearch, #stepAnalysis, #stepSave, #stepComplete, #stepError').addClass('d-none');
    }

    function showStep(stepId, prevStepId) {
        if (prevStepId) {
            $('#' + prevStepId).find('.step-indicator i').removeClass('fa-spinner fa-spin').addClass('fa-check-circle');
            $('#' + prevStepId).find('.step-indicator').addClass('success');
        }
        $('#' + stepId).removeClass('d-none');
    }

    function showPipelineError(msg) {
        $('#stepError').removeClass('d-none');
        $('#stepErrorMsg').text(msg);
    }

    // ─── Start Research ───
    $btnStart.on('click', function () {
        var query = $query.val().trim();
        if (!query) {
            abp.message.warn('Please enter a research query.', 'Warning');
            return;
        }

        var mode = $('input[name="researchMode"]:checked').val();

        // UI Preparation
        $btnStart.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-2"></i>Processing...');
        $resultsSection.addClass('d-none');
        resetPipeline();

        // Step animations
        $('#stepIdle').addClass('d-none');
        showStep('stepSearch');

        setTimeout(function () { showStep('stepResearch', 'stepSearch'); }, 800);
        setTimeout(function () { showStep('stepAnalysis', 'stepResearch'); }, 1600);
        setTimeout(function () { showStep('stepSave', 'stepAnalysis'); }, 2400);

        // API Call
        agentEcosystem.services.research.execute({
            query: query,
            mode: mode
        }).then(function (result) {
            // Pipeline completed
            showStep('stepComplete', 'stepSave');
            $('#stepCompleteTime').text(result.processingTimeMs + 'ms');

            // Display results
            displayResults(result, mode);

            // Refresh history
            loadHistory();

        }).catch(function (err) {
            showPipelineError(err.message || 'An unknown error occurred.');
        }).always(function () {
            $btnStart.prop('disabled', false).html('<i class="fas fa-rocket me-2"></i>Start Research');
        });
    });

    // ─── Display Results ───
    function displayResults(result, mode) {
        $resultsSection.removeClass('d-none');

        // Labels
        var modeLabel = mode === 'a2a' ? 'A2A Protocol' : 'ADK Sequential';
        $('#resultModeLabel').html('<i class="fas fa-tag me-1"></i>' + modeLabel);
        $('#resultTimeLabel').html('<i class="fas fa-clock me-1"></i>' + result.processingTimeMs + 'ms');

        // Analysis Report (simple Markdown → HTML conversion)
        $('#analysisContent').html(renderMarkdown(result.analysisResult || 'No results'));

        // Research Report
        $('#researchContent').html(renderMarkdown(result.researchReport || 'No results'));

        // Raw Data
        $('#rawContent').text(result.rawSearchResults || 'No data');

        // Agent Events
        var eventsHtml = '';
        if (result.agentEvents && result.agentEvents.length > 0) {
            result.agentEvents.forEach(function (evt) {
                var statusClass = evt.status === 'completed' ? 'success' : (evt.status === 'failed' ? 'danger' : 'info');
                var statusIcon = evt.status === 'completed' ? 'check-circle' : (evt.status === 'failed' ? 'times-circle' : 'info-circle');
                eventsHtml += '<div class="agent-event mb-2 p-3 border rounded">';
                eventsHtml += '<div class="d-flex align-items-center justify-content-between mb-1">';
                eventsHtml += '<strong><i class="fas fa-robot me-1"></i>' + (evt.agentName || 'Agent') + '</strong>';
                eventsHtml += '<span class="badge bg-' + statusClass + '"><i class="fas fa-' + statusIcon + ' me-1"></i>' + evt.status + '</span>';
                eventsHtml += '</div>';
                if (evt.content) {
                    var preview = evt.content.length > 300 ? evt.content.substring(0, 300) + '...' : evt.content;
                    eventsHtml += '<small class="text-muted">' + escapeHtml(preview) + '</small>';
                }
                eventsHtml += '</div>';
            });
        } else {
            eventsHtml = '<p class="text-muted">No agent events recorded.</p>';
        }
        $('#eventsContent').html(eventsHtml);

        // Scroll to results
        $resultsSection[0].scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    // ─── Research History ───
    function loadHistory() {
        $('#historyLoading').removeClass('d-none');
        $('#historyEmpty, #historyTable').addClass('d-none');

        agentEcosystem.services.research.getHistory()
            .then(function (items) {
                $('#historyLoading').addClass('d-none');

                if (!items || items.length === 0) {
                    $('#historyEmpty').removeClass('d-none');
                    return;
                }

                var tbody = '';
                items.forEach(function (item) {
                    var statusBadge = item.status === 'Completed'
                        ? '<span class="badge bg-success">Completed</span>'
                        : '<span class="badge bg-danger">Failed</span>';
                    var date = item.completedAt ? new Date(item.completedAt).toLocaleString('en-US') : '-';
                    var time = item.processingTimeMs ? item.processingTimeMs + 'ms' : '-';

                    tbody += '<tr>';
                    tbody += '<td class="fw-semibold">' + escapeHtml(item.query) + '</td>';
                    tbody += '<td>' + statusBadge + '</td>';
                    tbody += '<td><code>' + time + '</code></td>';
                    tbody += '<td class="text-muted small">' + date + '</td>';
                    tbody += '<td><button class="btn btn-sm btn-outline-primary btn-view-detail" data-id="' + item.id + '"><i class="fas fa-eye"></i></button></td>';
                    tbody += '</tr>';
                });

                $('#historyBody').html(tbody);
                $('#historyTable').removeClass('d-none');
            })
            .catch(function () {
                $('#historyLoading').addClass('d-none');
                $('#historyEmpty').removeClass('d-none');
            });
    }

    // Detail button
    $(document).on('click', '.btn-view-detail', function () {
        var id = $(this).data('id');
        agentEcosystem.services.research.getById(id).then(function (result) {
            if (result) {
                displayResults(result, result.mode || 'sequential');
            }
        });
    });

    // Refresh button
    $('#btnRefreshHistory').on('click', function () {
        loadHistory();
    });

    // ─── Helper Functions ───
    function renderMarkdown(text) {
        if (!text) return '';
        // Simple Markdown → HTML conversion
        var html = escapeHtml(text);
        // Headings
        html = html.replace(/^### (.+)$/gm, '<h5 class="mt-3 mb-2">$1</h5>');
        html = html.replace(/^## (.+)$/gm, '<h4 class="mt-4 mb-2 text-primary">$1</h4>');
        html = html.replace(/^# (.+)$/gm, '<h3 class="mt-4 mb-3">$1</h3>');
        // Bold
        html = html.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
        // Italic
        html = html.replace(/\*(.+?)\*/g, '<em>$1</em>');
        // Bullet points
        html = html.replace(/^- (.+)$/gm, '<li>$1</li>');
        html = html.replace(/(<li>.*<\/li>)/gs, '<ul class="mb-2">$1</ul>');
        // Numbered list
        html = html.replace(/^\d+\. (.+)$/gm, '<li>$1</li>');
        // Separator
        html = html.replace(/^---$/gm, '<hr>');
        // Line breaks
        html = html.replace(/\n\n/g, '</p><p>');
        html = html.replace(/\n/g, '<br>');
        return '<div class="markdown-body"><p>' + html + '</p></div>';
    }

    function escapeHtml(str) {
        if (!str) return '';
        return str.replace(/&/g, '&amp;')
                  .replace(/</g, '&lt;')
                  .replace(/>/g, '&gt;')
                  .replace(/"/g, '&quot;');
    }

    // Load history on page load
    loadHistory();
})();