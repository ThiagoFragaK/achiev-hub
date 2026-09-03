<template>
    <div :id="id" :aria-labelledby="`${id}-label`" class="modal fade" tabindex="-1" aria-hidden="true" ref="modalEl">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" :id="`${id}-label`">
                        {{ title }}
                    </h5>
                    <button
                        type="button"
                        class="btn-close"
                        data-bs-dismiss="modal"
                        aria-label="Close"
                        :disabled="isLoading"
                        @click="onDismiss"
                    />
                </div>

                <div class="modal-body">
                    <p v-if="message" class="mb-0" :class="{ 'mb-3': !!confirmWord }">{{ message }}</p>

                    <div v-if="confirmWord" :class="{ 'mt-3': !!message }">
                        <label class="form-label small fw-semibold" :for="`${id}-confirm-input`">
                            {{ confirmPrompt }}
                        </label>
                        <input
                            :id="`${id}-confirm-input`"
                            ref="confirmInput"
                            type="text"
                            class="form-control"
                            v-model="typedWord"
                            autocomplete="off"
                            :disabled="isLoading"
                            @keyup.enter="onConfirm"
                        />
                    </div>
                </div>

                <div class="modal-footer justify-content-center">
                    <button
                        type="button"
                        class="btn btn-outline-secondary"
                        data-bs-dismiss="modal"
                        :disabled="isLoading"
                        @click="onDismiss"
                    >
                        {{ cancelText }}
                    </button>

                    <button
                        type="button"
                        class="btn"
                        :class="confirmButtonClass"
                        :disabled="isLoading || !canConfirm"
                        @click="onConfirm"
                    >
                        <div style="min-width: 80px" class="text-center">
                            <span v-if="isLoading" class="spinner-grow spinner-grow-sm" role="status"></span>
                            <span v-else>{{ confirmText }}</span>
                        </div>
                    </button>
                </div>
            </div>
        </div>
    </div>
</template>

<script>
import { Modal } from 'bootstrap'

export default {
    name: 'ConfirmModal',
    props: {
        id: {
            type: String,
            required: true,
        },
        title: {
            type: String,
            default: '',
        },
        message: {
            type: String,
            default: '',
        },
        confirmText: {
            type: String,
            default: 'Confirm',
        },
        cancelText: {
            type: String,
            default: 'Cancel',
        },
        confirmWord: {
            type: String,
            default: '',
        },
        confirmPrompt: {
            type: String,
            default: '',
        },
        confirmVariant: {
            type: String,
            default: 'danger',
        },
        isLoading: {
            type: Boolean,
            default: false,
        },
    },
    emits: ['confirm', 'cancel'],
    data() {
        return {
            typedWord: '',
        };
    },
    computed: {
        canConfirm() {
            if (!this.confirmWord) {
                return true;
            }
            return this.typedWord === this.confirmWord;
        },
        confirmButtonClass() {
            return `btn-${this.confirmVariant}`;
        },
    },
    mounted() {
        this.modalInstance = new Modal(this.$refs.modalEl, {
            backdrop: 'static',
            keyboard: false,
        });

        this.$refs.modalEl.addEventListener('hidden.bs.modal', this.resetState);
    },
    beforeUnmount() {
        this.$refs.modalEl?.removeEventListener('hidden.bs.modal', this.resetState);
    },
    methods: {
        open() {
            this.typedWord = '';
            this.modalInstance?.show();
            if (this.confirmWord) {
                this.$nextTick(() => {
                    this.$refs.confirmInput?.focus();
                });
            }
        },
        close() {
            this.modalInstance?.hide();
        },
        resetState() {
            this.typedWord = '';
        },
        onDismiss() {
            this.$emit('cancel');
            this.close();
        },
        onConfirm() {
            if (!this.canConfirm || this.isLoading) {
                return;
            }
            this.$emit('confirm');
        },
    },
};
</script>
