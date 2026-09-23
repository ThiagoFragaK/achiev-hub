/**
 * Keeps an ApexCharts chart in step with its wrapper, so a CSS driven height
 * (clamp, vw, %) still reaches Apex as the pixel height it needs after a resize.
 *
 * The component using it must put ref="chartWrapper" on the element that carries
 * `wrapperHeight`, and hand `chartHeight` to <apexchart>.
 */
export const chartHeightMixin = {
    props: {
        height: {
            type: [Number, String],
            required: false,
            default: 220
        }
    },
    data() {
        return {
            measuredHeight: 0
        }
    },
    computed: {
        wrapperHeight() {
            return typeof this.height === 'number' ? `${this.height}px` : this.height
        },
        chartHeight() {
            if (this.measuredHeight > 0) {
                return this.measuredHeight
            }
            return typeof this.height === 'number' ? this.height : '100%'
        }
    },
    mounted() {
        const wrapper = this.$refs.chartWrapper
        if (!wrapper || typeof ResizeObserver === 'undefined') {
            return
        }

        this.chartResizeObserver = new ResizeObserver((entries) => {
            const measured = Math.round(entries[0]?.contentRect.height ?? 0)
            if (measured > 0) {
                this.measuredHeight = measured
            }
        })
        this.chartResizeObserver.observe(wrapper)
    },
    beforeUnmount() {
        this.chartResizeObserver?.disconnect()
        this.chartResizeObserver = null
    }
}
