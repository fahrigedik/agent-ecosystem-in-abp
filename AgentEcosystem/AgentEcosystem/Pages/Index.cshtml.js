(function () {
    // ═══════════════════════════════════════════════════
    // AI Agent Ecosystem — Frontend Controller
    // ═══════════════════════════════════════════════════

    var $query = $('#researchQuery');
    var $btnStart = $('#btnStartResearch');
    var $resultsSection = $('#resultsSection');

    // ─── Pipeline Adımları ───
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

    // ─── Araştırma Başlat ───
    $btnStart.on('click', function () {
        var query = $query.val().trim();
        if (!query) {
            abp.message.warn('Lütfen bir araştırma sorgusu girin.', 'Uyarı');
            return;
        }

        var mode = $('input[name="researchMode"]:checked').val();

        // UI Hazırlık
        $btnStart.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-2"></i>İşleniyor...');
        $resultsSection.addClass('d-none');
        resetPipeline();

        // Adım animasyonları
        $('#stepIdle').addClass('d-none');
        showStep('stepSearch');

        setTimeout(function () { showStep('stepResearch', 'stepSearch'); }, 800);
        setTimeout(function () { showStep('stepAnalysis', 'stepResearch'); }, 1600);
        setTimeout(function () { showStep('stepSave', 'stepAnalysis'); }, 2400);

        // API Çağrısı
        agentEcosystem.services.research.execute({
            query: query,
            mode: mode
        }).then(function (result) {
            // Pipeline tamamlandı
            showStep('stepComplete', 'stepSave');
            $('#stepCompleteTime').text(result.processingTimeMs + 'ms');

            // Sonuçları göster
            displayResults(result, mode);

            // Geçmişi yenile
            loadHistory();

        }).catch(function (err) {
            showPipelineError(err.message || 'Bilinmeyen hata oluştu.');
        }).always(function () {
            $btnStart.prop('disabled', false).html('<i class="fas fa-rocket me-2"></i>Araştırmayı Başlat');
        });
    });

    // ─── Sonuçları Göster ───
    function displayResults(result, mode) {
        $resultsSection.removeClass('d-none');

        // Etiketler
        var modeLabel = mode === 'a2a' ? 'A2A Protocol' : 'ADK Sequential';
        $('#resultModeLabel').html('<i class="fas fa-tag me-1"></i>' + modeLabel);
        $('#resultTimeLabel').html('<i class="fas fa-clock me-1"></i>' + result.processingTimeMs + 'ms');

        // Analiz Raporu (Markdown → HTML basit dönüşüm)
        $('#analysisContent').html(renderMarkdown(result.analysisResult || 'Sonuç yok'));

        // Araştırma Raporu
        $('#researchContent').html(renderMarkdown(result.researchReport || 'Sonuç yok'));

        // Ham Veri
        $('#rawContent').text(result.rawSearchResults || 'Veri yok');

        // Ajan Olayları
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
            eventsHtml = '<p class="text-muted">Ajan olayı kaydedilmedi.</p>';
        }
        $('#eventsContent').html(eventsHtml);

        // Sonuca scroll
        $resultsSection[0].scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    // ─── Geçmiş Araştırmalar ───
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
                        ? '<span class="badge bg-success">Tamamlandı</span>'
                        : '<span class="badge bg-danger">Hatalı</span>';
                    var date = item.completedAt ? new Date(item.completedAt).toLocaleString('tr-TR') : '-';
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

    // Detay butonu
    $(document).on('click', '.btn-view-detail', function () {
        var id = $(this).data('id');
        agentEcosystem.services.research.getById(id).then(function (result) {
            if (result) {
                displayResults(result, result.mode || 'sequential');
            }
        });
    });

    // Yenile butonu
    $('#btnRefreshHistory').on('click', function () {
        loadHistory();
    });

    // ─── Yardımcı Fonksiyonlar ───
    function renderMarkdown(text) {
        if (!text) return '';
        // Basit Markdown → HTML dönüşümü
        var html = escapeHtml(text);
        // Başlıklar
        html = html.replace(/^### (.+)$/gm, '<h5 class="mt-3 mb-2">$1</h5>');
        html = html.replace(/^## (.+)$/gm, '<h4 class="mt-4 mb-2 text-primary">$1</h4>');
        html = html.replace(/^# (.+)$/gm, '<h3 class="mt-4 mb-3">$1</h3>');
        // Kalın
        html = html.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
        // İtalik
        html = html.replace(/\*(.+?)\*/g, '<em>$1</em>');
        // Madde işareti
        html = html.replace(/^- (.+)$/gm, '<li>$1</li>');
        html = html.replace(/(<li>.*<\/li>)/gs, '<ul class="mb-2">$1</ul>');
        // Numaralı liste
        html = html.replace(/^\d+\. (.+)$/gm, '<li>$1</li>');
        // Ayraç
        html = html.replace(/^---$/gm, '<hr>');
        // Satır sonları
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

    // Sayfa yüklendiğinde geçmişi yükle
    loadHistory();
})();