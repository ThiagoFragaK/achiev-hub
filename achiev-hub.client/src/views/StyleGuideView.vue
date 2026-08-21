<template>
    <div class="min-vh-100 py-5">
        <div class="container" style="max-width: 56rem">
            <header
                class="d-flex flex-column flex-sm-row align-items-sm-start justify-content-between gap-3 mb-5"
            >
                <div>
                    <h1 class="display-6 mb-2">Style Guide</h1>
                    <p class="text-secondary mb-0">Custom Bootstrap theme used across the app.</p>
                </div>
                <RouterLink to="/" class="btn btn-outline-primary"> Back to Home </RouterLink>
            </header>

            <section class="mb-5">
                <h2 class="h4 mb-3">Typography</h2>
                <div class="card">
                    <div class="card-body">
                        <h1>Heading 1</h1>
                        <h2>Heading 2</h2>
                        <h3>Heading 3</h3>
                        <p>Body text for primary content and descriptions.</p>
                        <p class="text-secondary mb-0">
                            Muted text for secondary details and helper copy.
                        </p>
                    </div>
                </div>
            </section>

            <section class="mb-5">
                <h2 class="h4 mb-3">Buttons</h2>
                <div class="d-flex flex-wrap gap-2">
                    <button
                        v-for="variant in buttonVariants"
                        :key="variant.label"
                        type="button"
                        class="btn"
                        :class="variant.className"
                    >
                        {{ variant.label }}
                    </button>
                </div>
            </section>

            <section class="mb-5">
                <h2 class="h4 mb-3">Badges</h2>
                <div class="d-flex flex-wrap gap-2">
                    <span
                        v-for="variant in badgeVariants"
                        :key="variant.label"
                        class="badge"
                        :class="variant.className"
                    >
                        {{ variant.label }}
                    </span>
                </div>
            </section>

            <section class="mb-5">
                <h2 class="h4 mb-3">Notifications</h2>
                <div class="d-flex flex-column gap-2">
                    <div
                        v-for="variant in alertVariants"
                        :key="variant.label"
                        class="alert d-flex align-items-start gap-2 mb-0"
                        :class="variant.className"
                        role="alert"
                    >
                        <Info :size="18" class="flex-shrink-0 mt-1" />
                        <div>
                            <strong class="d-block">{{ variant.label }} notification</strong>
                            Sample alert using Bootstrap {{ variant.label.toLowerCase() }} style.
                        </div>
                    </div>
                </div>
            </section>

            <section class="mb-5">
                <h2 class="h4 mb-3">Form</h2>
                <div class="card">
                    <div class="card-body">
                        <h3 class="h5">Sample input</h3>
                        <p class="text-secondary">Input and button pairing used in forms.</p>
                        <form class="row g-2" @submit.prevent>
                            <div class="col-sm">
                                <input
                                    type="text"
                                    name="sample"
                                    class="form-control"
                                    placeholder="Enter a value"
                                />
                            </div>
                            <div class="col-sm-auto">
                                <button type="submit" class="btn btn-primary w-100">Submit</button>
                            </div>
                        </form>
                    </div>
                </div>
            </section>

            <section class="mb-5">
                <h2 class="h4 mb-3">Card</h2>
                <div class="card">
                    <div class="card-body">
                        <h3 class="h5 card-title">Achievement card</h3>
                        <p class="card-text text-secondary">
                            Cards group related content and actions.
                        </p>
                        <p>Use cards for lookup results, game summaries, and settings panels.</p>
                        <div class="d-flex flex-wrap gap-2">
                            <span class="badge text-bg-success">Unlocked</span>
                            <span class="badge text-bg-secondary">Rare</span>
                        </div>
                    </div>
                </div>
            </section>

            <section>
                <h2 class="h4 mb-3">Table</h2>
                <div class="card">
                    <div class="card-body">
                        <div class="table-responsive">
                            <table
                                class="table table-bordered table-hover text-center mb-0 align-middle"
                            >
                                <thead class="table-primary">
                                    <tr>
                                        <th scope="col">Game</th>
                                        <th scope="col">Progress</th>
                                        <th scope="col">Status</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr v-for="game in sampleGames" :key="game.name">
                                        <td>
                                            {{ game.name }}
                                        </td>
                                        <td>
                                            {{ game.progress }}
                                        </td>
                                        <td>
                                            {{ game.status }}
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </section>
        </div>
    </div>
</template>

<script>
import { Info } from '@lucide/vue'

export default {
    name: 'StyleGuideView',
    components: {
        Info
    },
    data() {
        return {
            sampleGames: [
                { name: 'Hades', progress: '92%', status: 'Nearly done' },
                { name: 'Celeste', progress: '100%', status: 'Complete' },
                { name: 'Hollow Knight', progress: '64%', status: 'In progress' },
                { name: 'Dead Cells', progress: '18%', status: 'Started' }
            ],
            buttonVariants: [
                { label: 'Primary', className: 'btn-primary' },
                { label: 'Secondary', className: 'btn-secondary' },
                { label: 'Success', className: 'btn-success' },
                { label: 'Danger', className: 'btn-danger' },
                { label: 'Warning', className: 'btn-warning' },
                { label: 'Info', className: 'btn-info' },
                { label: 'Light', className: 'btn-light' },
                { label: 'Dark', className: 'btn-dark' },
                { label: 'Outline', className: 'btn-outline-primary' },
                { label: 'Link', className: 'btn-link' }
            ],
            badgeVariants: [
                { label: 'Primary', className: 'text-bg-primary' },
                { label: 'Secondary', className: 'text-bg-secondary' },
                { label: 'Success', className: 'text-bg-success' },
                { label: 'Danger', className: 'text-bg-danger' },
                { label: 'Warning', className: 'text-bg-warning' },
                { label: 'Info', className: 'text-bg-info' },
                { label: 'Light', className: 'text-bg-light' },
                { label: 'Dark', className: 'text-bg-dark' }
            ],
            alertVariants: [
                { label: 'Primary', className: 'alert-primary' },
                { label: 'Secondary', className: 'alert-secondary' },
                { label: 'Success', className: 'alert-success' },
                { label: 'Danger', className: 'alert-danger' },
                { label: 'Warning', className: 'alert-warning' },
                { label: 'Info', className: 'alert-info' }
            ]
        }
    }
}
</script>
