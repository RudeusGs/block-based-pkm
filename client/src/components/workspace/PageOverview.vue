<template>
  <section class="page-overview mb-4">
    <div class="d-flex flex-column flex-md-row justify-content-between align-items-start gap-4">
      <div class="overview-text">
        <h1 class="page-title mb-3">{{ title }}</h1>
        <p class="page-description mb-0">
          {{ description }}
        </p>
      </div>
    </div>
  </section>
</template>

<script setup>
defineProps({
  title: {
    type: String,
    default: ''
  },
  description: {
    type: String,
    default: ''
  }
})

defineEmits(['create-page'])
</script>

<style scoped>
.page-overview {
  margin-bottom: 1rem;
}

.overview-text {
  max-width: 720px;
}

.page-title {
  font-size: 2.25rem;
  font-weight: 800;
  letter-spacing: -0.04em;
  color: #e7e5e5;
}

.page-description {
  font-size: 1.05rem;
  line-height: 1.8;
  color: #acabaa;
  font-weight: 300;
}

.btn-page-create:hover {
  background: #252626;
  color: #c3d4ff;
}

@media (max-width: 768px) {
  .page-title {
    font-size: 1.75rem;
  }

  .page-description {
    font-size: 0.95rem;
  }
}
</style>