<template>
  <aside class="right-sidebar d-flex flex-column">
    <div class="p-4 border-bottom border-outline-soft">
      <h2 class="sidebar-title mb-1">Collaborators</h2>
      <p class="sidebar-subtitle mb-0">4 Online Now</p>
    </div>

    <div class="flex-grow-1 overflow-auto p-4">
      <div class="mb-5">
        <div class="section-label text-online mb-3">Online</div>

        <div class="d-flex flex-column gap-3">
          <div
            v-for="user in onlineUsers"
            :key="user.name"
            class="d-flex align-items-center gap-3"
          >
            <div class="position-relative">
              <img
                :src="user.avatar"
                :alt="user.name"
                class="avatar rounded-circle object-fit-cover"
              />
              <span class="online-dot"></span>
            </div>

            <div class="d-flex flex-column">
              <span class="user-name">{{ user.name }}</span>
              <span
                class="user-status"
                :class="{ 'typing-status': user.status === 'Typing...' }"
              >
                {{ user.status }}
              </span>
            </div>
          </div>
        </div>
      </div>

      <div class="mb-5">
        <div class="section-label text-muted-custom mb-3">Offline</div>

        <div class="d-flex flex-column gap-3 offline-group">
          <div
            v-for="user in offlineUsers"
            :key="user.name"
            class="d-flex align-items-center gap-3"
          >
            <img
              :src="user.avatar"
              :alt="user.name"
              class="avatar rounded-circle object-fit-cover"
            />
            <span class="user-name">{{ user.name }}</span>
          </div>
        </div>
      </div>

      <div class="pt-4 border-top border-outline-soft">
        <div class="section-label text-muted-custom mb-4">Recent Activity</div>

        <div class="d-flex flex-column gap-4">
          <div
            v-for="(activity, index) in activities"
            :key="index"
            class="timeline-item position-relative"
          >
            <div class="timeline-dot" :class="{ active: activity.active }"></div>
            <p class="timeline-text mb-1" v-html="activity.text"></p>
            <span class="timeline-time">{{ activity.time }}</span>
          </div>
        </div>
      </div>
    </div>
  </aside>
</template>

<script setup>
const onlineUsers = [
  {
    name: 'John Doe',
    status: 'Typing...',
    avatar:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuBRalr25iyreR3mTYkgbZzm2sdy0YsT-Zge2REFgpLMddHoB76wT3CgskuKJlXRr3XWOGvjLVY2fV4O0vqg1m9pdfWb1527j9tltLokuZNEy2yH_2UhYp-9Ika2LePiKwzqOQbPRvi911wKpUq11q4cIFjDp2EuK64DWwkj9JVwMGRIBIO9RDsUK2u-i5huYyM0dJ7bfyFL9brNm9N8JqO5lwBvFpz6jiEMIINBQIcWjk1DUEuYl-nfvCo07bkawieptWttzmll75g'
  },
  {
    name: 'Sarah Chen',
    status: 'Viewing Roadmap',
    avatar:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuCsHgUG4M9NRow-nrl-MDcnWfgQs9ZbT_xs11AaReIZE9DtEbicgf6nFWtUpFyQ6AfpUNLQvS-CWR-93EV1QNmdrhCY8gxTbTZrISKl4QPdqWEEjoWBWkkUH0Rpibx5C4rQ4d8HVRXd8T8_9ccizVF7h79Ffwdk7KLibcLidfh5uXICvpEPPXGZj-cRYoq2N4mivUBt6OlACtXa_jRriNtAyyM7pgv1_b_AqHu3gF7GG65xZz_N0ZebaNyQT9gx861hMScQ6hKI3H4'
  },
  {
    name: 'Mike Ross',
    status: 'Idle',
    avatar:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuA2vuVbr7PMA8nfok82S_wxHC25gG4ay7Fs7SVqhmmcZhuIjrrKxx9V1B4Nby016goQ-jFJSTcXITJS9y01G73EuOs_nY_yVnFqqh9eQudOYiOamakbolddl31zq04_d4HmJTbSrXRcgaxj31kSKbFsi4948vf7419XrfxADg2fuuN5cr30Uh_qapij4ZlbtnQcUTldninRzEKvb2XXA4fBrcLCdKagWbWwNMmk79Q5D4Cd9lWUMjpUkj9mqXvSkQGargeT8qyLdDM'
  }
]

const offlineUsers = [
  {
    name: 'Emily Blunt',
    avatar:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuCJVDAE9oCyUnHpr8sii2BD5rhvu85GupJfK6k3HSPhx8-DQS33q3cmmNZ40Gqra3Gpn6rl7SP8txX9rpFr0phpesDlOB7U4gfA6L18rRYnPXoDtnH2lB80sZ9P8GLs8CEBlCSRVcgu5uYTWLpwNQPrzF9lfIoasyz8uaUXexp54oCjDCIldvWtW4TKVbmGN8IrBwe5P5PC8xsfTCgp6pM_Dh1D_Eg0a9pKfNaggyv9RrkNmsWpXmREFfYhpVLfeq3mTZXx0eFruj4'
  },
  {
    name: 'David Gandy',
    avatar:
      'https://lh3.googleusercontent.com/aida-public/AB6AXuCbTAU74kRy7e6fCEFaMyN8uRa8nr7T2HFiauaG5qI8TjPlmeOU6wFFfpC3q6PBAXir6elKKpGDkMrMcvCbVS-VfKpUCkA9myssOSxLrpmIKPIz_DIU-QhXlPOhj8xRohrUMxggT1v-axMsL-RZBuusECxFHxWIPZs7-7785iXk1dx0_4awyiiIWCPIdzKqQnx4bUkVSuDMtr55DTC8hA1kI-BTPrlYlNcvbzjqcplSvqbzVs3zV7UAUJ1Lz5s2DAjQHavdmV1cF5w'
  }
]

const activities = [
  {
    text: 'Sarah Chen updated the <span class="highlight-text">Sprint Goal</span>',
    time: '12 mins ago',
    active: true
  },
  {
    text: "Task 'Client Meeting' marked as <span class='muted-inline'>Complete</span>",
    time: '2 hours ago',
    active: false
  }
]
</script>

<style scoped>
.right-sidebar {
  width: 320px;
  height: 100vh;
  position: fixed;
  top: 0;
  right: 0;
  z-index: 1040;
  background: rgba(19, 19, 19, 0.92);
  backdrop-filter: blur(18px);
  border-left: 1px solid rgba(72, 72, 72, 0.15);
  color: #e7e5e4;
}

.sidebar-title {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: #e7e5e4;
}

.sidebar-subtitle {
  font-size: 11px;
  color: #acabaa;
}

.section-label {
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.text-online {
  color: #c180ff;
}

.text-muted-custom {
  color: rgba(172, 171, 170, 0.45);
}

.avatar {
  width: 36px;
  height: 36px;
}

.user-name {
  font-size: 13px;
  font-weight: 600;
  color: #e7e5e4;
}

.user-status {
  font-size: 11px;
  color: #acabaa;
}

.typing-status {
  color: #c180ff;
  animation: pulseText 1.4s infinite;
}

.online-dot {
  position: absolute;
  right: 0;
  bottom: 0;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background-color: #22c55e;
  border: 2px solid #131313;
  box-shadow: 0 0 8px rgba(34, 197, 94, 0.5);
}

.offline-group {
  opacity: 0.55;
  filter: grayscale(1);
  transition: 0.2s ease;
}

.offline-group:hover {
  opacity: 1;
  filter: grayscale(0);
}

.timeline-item {
  padding-left: 20px;
  border-left: 1px solid rgba(72, 72, 72, 0.2);
}

.timeline-dot {
  position: absolute;
  left: -5px;
  top: 4px;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background-color: #484848;
  box-shadow: 0 0 0 4px #131313;
}

.timeline-dot.active {
  background-color: #bdc2ff;
}

.timeline-text {
  font-size: 12px;
  line-height: 1.4;
  color: #e7e5e4;
}

.timeline-time {
  font-size: 10px;
  color: #acabaa;
}

.highlight-text {
  color: #bdc2ff;
}

.muted-inline {
  color: #acabaa;
}

.border-outline-soft {
  border-color: rgba(72, 72, 72, 0.15) !important;
}

@keyframes pulseText {
  0%,
  100% {
    opacity: 1;
  }
  50% {
    opacity: 0.45;
  }
}

@media (max-width: 1200px) {
  .right-sidebar {
    display: none !important;
  }
}
</style>