<template>
    <ModalComponent
        ref="modal"
        id="goal-achievements-modal"
        :title="modalTitle"
        size="lg"
        :scrollable="true"
        :isLoading="saving"
        @cancel="onCancel"
    >
        <p v-if="loadError" class="alert alert-danger py-2" role="alert">{{ loadError }}</p>
        <p v-else-if="loading" class="text-secondary small mb-3">Loading achievements…</p>

        <div v-else class="mb-3">
            <label class="form-label" for="goal-modal-percentage">End goal %</label>
            <div class="d-flex align-items-center gap-2">
                <input
                    id="goal-modal-percentage"
                    :value="percentageGoal"
                    type="number"
                    class="form-control"
                    :class="{ 'is-invalid': isEditing && percentageInvalid }"
                    min="0"
                    max="100"
                    step="0.1"
                    readonly
                    :disabled="saving || !isEditing"
                />
                <button
                    v-if="canEdit && !isEditing"
                    type="button"
                    class="btn btn-primary flex-shrink-0"
                    @click="startEditing"
                >
                    Edit
                </button>
            </div>
            <div v-if="isEditing" class="form-text">
                Auto-set from selected achievements
                ({{ gameInfo.unlocked }} unlocked + {{ selectedIds.size }} selected) / {{ gameInfo.total }}.
            </div>
            <div v-if="isEditing && percentageInvalid" class="invalid-feedback d-block">
                Goal % must be above current progress ({{ formatPercentage(gameInfo.percentage) }}) and at most 100.
            </div>
        </div>

        <div v-if="!loading && !loadError" class="table-responsive">
            <p v-if="rows.length === 0" class="text-secondary small mb-0">
                No achievements attached to this goal yet.
            </p>
            <table v-else class="table table-sm table-bordered align-middle mb-0">
                <thead class="table-primary">
                    <tr>
                        <th v-if="isEditing" style="width: 2.5rem">
                            <input
                                type="checkbox"
                                class="form-check-input"
                                :checked="allSelected"
                                :disabled="saving"
                                @change="toggleAll"
                            />
                        </th>
                        <th>Achievement</th>
                        <th>Status</th>
                        <th>Global %</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="row in rows" :key="row.achievementId">
                        <td v-if="isEditing">
                            <input
                                type="checkbox"
                                class="form-check-input"
                                :checked="selectedIds.has(row.achievementId)"
                                :disabled="saving"
                                @change="toggleRow(row.achievementId)"
                            />
                        </td>
                        <td>
                            <div class="d-flex align-items-center gap-2">
                                <img
                                    v-if="row.icon"
                                    :src="row.icon"
                                    alt=""
                                    width="20"
                                    height="20"
                                    class="rounded"
                                />
                                <div>
                                    <div class="fw-medium">{{ row.name }}</div>
                                    <div v-if="row.description" class="small text-secondary">
                                        {{ row.description }}
                                    </div>
                                </div>
                            </div>
                        </td>
                        <td>
                            <span
                                class="badge"
                                :class="row.status === 1 || row.status === 'Completed' || row.unlocked
                                    ? 'text-bg-success'
                                    : 'text-bg-secondary'"
                            >
                                {{ statusLabel(row) }}
                            </span>
                        </td>
                        <td>
                            <span :class="globalPercentageColorClass(row.globalPercentage)">
                                {{ formatGlobalPercentage(row.globalPercentage) }}
                            </span>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>

        <p v-if="saveError" class="text-danger small mt-3 mb-0">{{ saveError }}</p>

        <template #footer>
            <div class="modal-footer justify-content-center">
                <button
                    v-if="isEditing"
                    type="button"
                    class="btn btn-outline-secondary"
                    :disabled="saving"
                    @click="cancelEditing"
                >
                    Cancel
                </button>
                <button
                    v-else
                    type="button"
                    class="btn btn-outline-secondary"
                    data-bs-dismiss="modal"
                    :disabled="saving"
                    @click="onCancel"
                >
                    Close
                </button>
                <button
                    v-if="isEditing"
                    type="button"
                    class="btn btn-primary"
                    :disabled="saving || !canSave"
                    @click="onSave"
                >
                    <div style="min-width: 80px" class="text-center">
                        <span
                            v-if="saving"
                            class="spinner-grow spinner-grow-sm"
                            role="status"
                        ></span>
                        <span v-else>Save</span>
                    </div>
                </button>
            </div>
        </template>
    </ModalComponent>
</template>

<script>
import ModalComponent from '@/components/global/ModalComponent.vue'
import { formatGlobalPercentage, globalPercentageColorClass } from '@/lib/globalPercentage'
import { getGoalAchievements, replaceGoalAchievements } from '@/services/goalsService'
import { getGameProgress } from '@/services/statsService'
import { formatPercentage } from '@/utils/NumberFormat'

const STATUS = {
    Active: 0,
    Completed: 1,
    Abandoned: 2
}

function roundPercent(value) {
    return Math.round(value * 100) / 100
}

export default {
    name: 'GoalAchievementsModal',
    components: {
        ModalComponent
    },
    emits: ['saved'],
    data() {
        return {
            goal: null,
            rows: [],
            selectedIds: new Set(),
            percentageGoal: null,
            loading: false,
            saving: false,
            isEditing: false,
            loadError: '',
            saveError: '',
            gameInfo: {
                total: 0,
                unlocked: 0,
                percentage: 0
            }
        }
    },
    computed: {
        modalTitle() {
            return this.goal?.gameName
                ? `Goal achievements — ${this.goal.gameName}`
                : 'Goal achievements'
        },
        canEdit() {
            const status = this.goal?.status
            return status === STATUS.Active || status === 'Active'
        },
        allSelected() {
            return this.rows.length > 0 && this.selectedIds.size === this.rows.length
        },
        percentageInvalid() {
            if (this.percentageGoal == null || this.percentageGoal === '') {
                return true
            }
            const value = Number(this.percentageGoal)
            return !Number.isFinite(value)
                || value <= this.gameInfo.percentage
                || value > 100
        },
        canSave() {
            return !!this.goal?.id
                && this.isEditing
                && this.canEdit
                && !this.loading
                && !this.percentageInvalid
                && !this.saving
        }
    },
    methods: {
        formatPercentage,
        formatGlobalPercentage,
        globalPercentageColorClass,
        statusLabel(row) {
            if (row.unlocked || row.status === 1 || row.status === 'Completed') {
                return 'Completed'
            }
            return 'Pending'
        },
        syncPercentageGoal() {
            const total = Number(this.gameInfo.total) || 0
            const unlocked = Number(this.gameInfo.unlocked) || 0
            const selected = this.selectedIds.size
            if (total <= 0) {
                this.percentageGoal = this.goal?.percentageGoal ?? null
                return
            }
            if (selected <= 0) {
                const fallback = Math.min(100, roundPercent(this.gameInfo.percentage + 0.01))
                this.percentageGoal = fallback > this.gameInfo.percentage ? fallback : 100
                return
            }
            this.percentageGoal = Math.min(100, roundPercent(((unlocked + selected) / total) * 100))
        },
        resetSelectionFromRows() {
            this.selectedIds = new Set(this.rows.map((r) => r.achievementId))
            this.syncPercentageGoal()
        },
        startEditing() {
            if (!this.canEdit) {
                return
            }
            this.saveError = ''
            this.isEditing = true
        },
        cancelEditing() {
            this.saveError = ''
            this.resetSelectionFromRows()
            this.isEditing = false
        },
        async open(goal) {
            this.goal = goal
            this.rows = []
            this.selectedIds = new Set()
            this.percentageGoal = goal?.percentageGoal ?? null
            this.gameInfo = { total: 0, unlocked: 0, percentage: 0 }
            this.isEditing = false
            this.loadError = ''
            this.saveError = ''
            this.$refs.modal?.open()
            await this.load()
        },
        close() {
            this.$refs.modal?.close()
        },
        async load() {
            if (!this.goal?.id) {
                return
            }
            this.loading = true
            this.loadError = ''
            try {
                const [rows, progress] = await Promise.all([
                    getGoalAchievements(this.goal.id),
                    this.goal.appId ? getGameProgress(this.goal.appId) : Promise.resolve(null)
                ])
                this.rows = rows || []
                this.selectedIds = new Set(this.rows.map((r) => r.achievementId))
                const total = Number(progress?.totalCount) || 0
                const unlocked = Number(progress?.unlockedCount) || 0
                this.gameInfo = {
                    total,
                    unlocked,
                    percentage: total > 0 ? roundPercent((unlocked / total) * 100) : 0
                }
                this.syncPercentageGoal()
            } catch (err) {
                this.rows = []
                this.selectedIds = new Set()
                this.loadError = err?.message || 'Failed to load goal achievements.'
            } finally {
                this.loading = false
            }
        },
        toggleRow(achievementId) {
            if (!this.isEditing) {
                return
            }
            const next = new Set(this.selectedIds)
            if (next.has(achievementId)) {
                next.delete(achievementId)
            } else {
                next.add(achievementId)
            }
            this.selectedIds = next
            this.syncPercentageGoal()
        },
        toggleAll(event) {
            if (!this.isEditing) {
                return
            }
            if (event.target.checked) {
                this.selectedIds = new Set(this.rows.map((r) => r.achievementId))
            } else {
                this.selectedIds = new Set()
            }
            this.syncPercentageGoal()
        },
        onCancel() {
            this.saveError = ''
            this.isEditing = false
        },
        async onSave() {
            if (!this.canSave) {
                return
            }
            this.saving = true
            this.saveError = ''
            try {
                await replaceGoalAchievements(this.goal.id, {
                    achievementIds: [...this.selectedIds],
                    percentageGoal: Number(this.percentageGoal)
                })
                this.$emit('saved')
                this.close()
            } catch (err) {
                this.saveError = err?.message || 'Failed to save goal achievements.'
            } finally {
                this.saving = false
            }
        }
    }
}
</script>
