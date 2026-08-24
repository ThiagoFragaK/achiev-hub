<template>
    <div v-if="src" :class="['ratio', `ratio-${ratio}`]">
        <img :src="src" :alt="alt" class="border rounded object-fit-cover" />
    </div>
    <div
        v-else
        :class="['bg-secondary-subtle', 'border', 'rounded', 'ratio', `ratio-${ratio}`]"
        aria-hidden="true"
    />
</template>

<script>
export default {
    name: 'ImageComponent',
    props: {
        src: {
            type: String,
            required: false,
            default: ''
        },
        alt: {
            type: String,
            required: false,
            default: ''
        },
        ratio: {
            type: String,
            required: false,
            default: '1x1'
        }
    }
}
</script>
