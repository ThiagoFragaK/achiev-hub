<template>
    <div v-if="status" class="d-flex justify-content-center align-items-center">
        <div :class="['spinner-border', `text-${color}`]" role="status">
            <span class="visually-hidden">Loading...</span>
        </div>
    </div>
</template>
  
<script>
export default {
    name: "LoadingComponent",
    props: {
        status: {
            type: Boolean,
            required: false,
            default: true,
        },
        color: {
            type: String,
            required: false,
            default: "primary",
        },
    },
};
</script>
  
<style scoped>
    .d-flex {
        min-height: 100px;
    }
</style>
