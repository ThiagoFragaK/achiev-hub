<template>
    <span class="badge" :class="'text-bg-' + color">
        <slot>{{ label }}</slot>
    </span>
</template>

<script>
export default {
    name: 'BadgeComponent',
    props: {
        label: {
            type: String,
            required: false,
            default: ''
        },
        color: {
            type: String,
            required: false,
            default: 'primary'
        }
    }
}
</script>
