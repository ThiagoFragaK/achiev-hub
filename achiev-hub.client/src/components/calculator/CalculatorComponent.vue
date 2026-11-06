<template>
    <AppShell>
        <h1 class="lastica-h3 mb-4">Calculator</h1>
        <AverageWhatIfCard />
    </AppShell>
</template>

<script>
import AverageWhatIfCard from './AverageWhatIfCard.vue';
import AppShell from '../layout/AppShell.vue';

export default {
    name: 'CalculatorComponent',
    components: {
        AverageWhatIfCard,
        AppShell
    },
}
</script>
