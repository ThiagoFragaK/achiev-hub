<template>
    <div class="card">
        <div class="card-body">
            <h2 class="h5 mb-3">{{ title }}</h2>
            <div class="table-responsive">
                <table class="table table-bordered table-sm text-center mb-0">
                    <thead class="table-primary">
                        <tr>
                            <th v-for="game in games" :key="game.name" scope="col">
                                {{ game.name }}
                            </th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td v-for="game in games" :key="`${game.name}-hours`">
                                {{ game.hours }}
                            </td>
                        </tr>
                        <tr>
                            <td v-for="game in games" :key="`${game.name}-percentage`">
                                {{ game.percentage }}
                            </td>
                        </tr>
                        <tr>
                            <td v-for="game in games" :key="`${game.name}-status`">
                                {{ game.status }}
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</template>

<script>
export default {
    name: 'RecentGamesTable',
    props: {
        title: {
            type: String,
            required: false,
            default: '76hrs in last 14 days'
        },
        games: {
            type: Array,
            required: true
        }
    }
}
</script>
