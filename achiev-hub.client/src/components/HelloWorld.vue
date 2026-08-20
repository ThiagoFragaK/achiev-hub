<template>
    <div class="greetings">
        <h1>{{ msg }}</h1>
        <p>AchievHub API is ready. Use the /api endpoints from the Vue services layer.</p>
    </div>
</template>

<script lang="js">
    import { defineComponent } from 'vue';

    export default defineComponent({
        props: {
            msg: {
                type: String,
                required: true
            }
        }
    });
</script>

<style scoped>
h1 {
    font-weight: 500;
    font-size: 2.6rem;
    position: relative;
    top: -10px;
}

h1, p {
    text-align: center;
}

@media (min-width: 1024px) {
    h1, p {
        text-align: left;
    }
}
</style>
