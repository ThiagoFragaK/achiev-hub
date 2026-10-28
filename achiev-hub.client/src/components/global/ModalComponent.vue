<template>
    <div
        :id="id"
        :aria-labelledby="`${id}-label`"
        class="modal fade"
        tabindex="-1"
        aria-hidden="true"
        ref="modalEl"
    >
        <div
            class="modal-dialog modal-dialog-centered"
            :class="[sizeClass, { 'modal-dialog-scrollable': scrollable }]"
        >
            <div class="modal-content border border-primary border-2">
                <slot name="header">
                    <div class="modal-header">
                        <h5
                            class="modal-title"
                            :id="`${id}-label`"
                        >
                            {{ title }}
                        </h5>
                        <button
                            type="button"
                            class="btn-close"
                            data-bs-dismiss="modal"
                            aria-label="Close"
                            :disabled="isLoading"
                            @click="$emit('cancel')"
                        />
                    </div>
                </slot>

                <slot name="body">
                    <div class="modal-body">
                        <slot />
                    </div>
                </slot>

                <slot name="footer">
                    <div class="modal-footer justify-content-center">
                        <button
                            type="button"
                            class="btn btn-outline-secondary"
                            data-bs-dismiss="modal"
                            :disabled="isLoading"
                            @click="onCancel"
                        >
                            {{ cancelText }}
                        </button>

                        <button
                            v-if="showSave"
                            type="button"
                            class="btn btn-primary"
                            :disabled="isLoading || saveDisabled"
                            @click="$emit('save')"
                        >
                            <div
                                style="min-width: 80px"
                                class="text-center"
                            >
                                <span
                                    v-if="isLoading"
                                    class="spinner-grow spinner-grow-sm"
                                    role="status"
                                ></span>
                                <span v-else>{{ saveText }}</span>
                            </div>
                        </button>
                    </div>
                </slot>
            </div>
        </div>
    </div>
</template>
<script>
import { Modal } from 'bootstrap'

export default {
    name: 'ModalComponent',
    props: {
        id: {
            type: String,
            required: true
        },
        isLoading: {
            type: Boolean,
            default: false
        },
        title: {
            type: String,
            default: ''
        },
        cancelText: {
            type: String,
            default: 'Cancel'
        },
        saveText: {
            type: String,
            default: 'Save'
        },
        showSave: {
            type: Boolean,
            default: true
        },
        saveDisabled: {
            type: Boolean,
            default: false
        },
        size: {
            type: String,
            default: '',
            validator: (value) => ['', 'sm', 'lg', 'xl'].includes(value)
        },
        scrollable: {
            type: Boolean,
            default: false
        }
    },
    emits: ['save', 'cancel'],
    computed: {
        sizeClass() {
            return this.size ? `modal-${this.size}` : ''
        }
    },
    mounted() {
        this.modalInstance = new Modal(this.$refs.modalEl, {
            backdrop: 'static',
            keyboard: false
        })
    },
    beforeUnmount() {
        this.modalInstance?.dispose()
    },
    methods: {
        open() {
            this.modalInstance?.show()
        },
        close() {
            this.modalInstance?.hide()
        },
        onCancel() {
            this.$emit('cancel')
            this.close()
        }
    }
}
</script>
